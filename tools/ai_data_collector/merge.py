"""Sentetik + sessions + golden birleştirici.

Kaynaklar (mevcutsa, sırayla):
  output/synthetic-<feature>.jsonl
  output/sessions-<feature>.jsonl
  golden/<feature>.jsonl

Çıktı:
  output/train-v1.jsonl   — prompt/completion formatında (trl/unsloth uyumlu)

Güvenceler:
  - Eval seti (tests/AiEvalDataset/v1/*.jsonl) ile input çakışması elenir (exact hash + cosine 0.95).
  - Kendi içinde cosine dedup 0.93.
  - Schema validation tekrar edilir (kaynaklar ayrı ayrı doğrulanmış olsa da paranoya).

Her output satırı:
  {
    "feature": "scaffold-project" | ...,
    "source":  "synthetic" | "sessions" | "golden",
    "messages": [
        {"role": "system", "content": "<feature-specific sistem prompt>"},
        {"role": "user", "content": "<user input as JSON>"},
        {"role": "assistant", "content": "<output JSON string>"}
    ]
  }

Çağrı:
  python -m tools.ai_data_collector.merge --out output/train-v1.jsonl
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from . import config
from .validation import scrub_json_values
from .validation.dedup import Dedup
from .validation.schema import SchemaError, validate

SYSTEM_PROMPTS = {
    "scaffold-project": (
        "Sen BitirmeProject AI agent'ısın. Kullanıcının serbest metin talebinden "
        "ProjectDraft JSON'u üretirsin. Yalnızca geçerli JSON döndürürsün; markdown "
        "fence, açıklama, İngilizce sızıntısı yasak. Türkçe yanıt ver."
    ),
    "enrich-issue": (
        "Sen BitirmeProject AI agent'ısın. Verilen issue başlığından detaylı "
        "issue spesifikasyonu üretirsin (description, acceptanceCriteria, edgeCases, "
        "storyPoints). Yalnızca geçerli JSON döndürürsün. Türkçe."
    ),
    "generate-plan": (
        "Sen BitirmeProject AI agent'ısın. Mevcut projeye yeni özellik sprint planı "
        "üretirsin. Yalnızca geçerli JSON (sprints[]) döndürürsün. Türkçe."
    ),
    # agent: sistem mesajı agent_synth.py içinde tam tool catalog'la birlikte üretiliyor;
    # buradan değil, örneğin kendi messages[0]'ından alınır.
    "agent": "",
}


def _read_jsonl(path: Path) -> list[dict]:
    if not path.exists():
        return []
    out: list[dict] = []
    with path.open(encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                out.append(json.loads(line))
            except json.JSONDecodeError:
                continue
    return out


def _input_text(ex: dict) -> str:
    inp = ex.get("input", {})
    if isinstance(inp, dict):
        return inp.get("description") or inp.get("title") or json.dumps(inp, ensure_ascii=False)
    return str(inp)


def _load_eval_inputs() -> dict[str, set[str]]:
    """Eval setindeki input metinlerini normalize edip feature başına sete alır."""
    result: dict[str, set[str]] = {f: set() for f in config.FEATURES}
    for feat in config.FEATURES:
        path = config.EVAL_DIR / f"{feat}.jsonl"
        for ex in _read_jsonl(path):
            result[feat].add(_normalize(_input_text(ex)))
    return result


def _normalize(s: str) -> str:
    return " ".join(s.lower().split())


def _to_messages(ex: dict) -> list[dict]:
    feat = ex["feature"]
    # agent: ham messages dizisini olduğu gibi kullan (multi-turn, system prompt zaten tam)
    if feat == "agent":
        return ex["messages"]
    sys = SYSTEM_PROMPTS[feat]
    user_content = json.dumps(ex["input"], ensure_ascii=False)
    assistant_content = json.dumps(ex["output"], ensure_ascii=False)
    return [
        {"role": "system", "content": sys},
        {"role": "user", "content": user_content},
        {"role": "assistant", "content": assistant_content},
    ]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=str(config.OUTPUT_DIR / "train-v1.jsonl"))
    ap.add_argument("--dedup-threshold", type=float, default=0.93)
    ap.add_argument("--eval-threshold", type=float, default=0.95)
    args = ap.parse_args()

    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    eval_inputs = _load_eval_inputs()
    eval_dedup: dict[str, Dedup] = {}
    for feat, texts in eval_inputs.items():
        d = Dedup(threshold=args.eval_threshold)
        d.load_existing(list(texts))
        eval_dedup[feat] = d

    train_dedup = Dedup(threshold=args.dedup_threshold)

    sources = [
        ("synthetic", config.OUTPUT_DIR),
        ("sessions", config.OUTPUT_DIR),
        ("golden", config.GOLDEN_DIR),
    ]

    stats = {
        "per_feature": {f: {"synthetic": 0, "sessions": 0, "golden": 0} for f in config.FEATURES},
        "drop_eval_exact": 0,
        "drop_eval_cosine": 0,
        "drop_schema": 0,
        "drop_dedup": 0,
        "drop_missing_fields": 0,
        "written": 0,
    }

    with out_path.open("w", encoding="utf-8") as fout:
        for source_name, base_dir in sources:
            for feat in config.FEATURES:
                if source_name == "golden":
                    src_path = base_dir / f"{feat}.jsonl"
                else:
                    src_path = base_dir / f"{source_name}-{feat}.jsonl"

                for ex in _read_jsonl(src_path):
                    feat_in = ex.get("feature", feat)
                    if feat_in != feat:
                        stats["drop_missing_fields"] += 1
                        continue

                    # agent: messages alanı olmalı; schema messages bütününü doğrular
                    if feat == "agent":
                        if "messages" not in ex:
                            stats["drop_missing_fields"] += 1
                            continue
                        try:
                            validate(feat, ex)
                        except SchemaError:
                            stats["drop_schema"] += 1
                            continue
                        # dedup için ilk user turn'ünü key olarak kullan
                        first_user = next((m.get("content", "") for m in ex["messages"] if m.get("role") == "user"), "")
                        inp_text = _normalize(first_user)
                    else:
                        if "input" not in ex or "output" not in ex:
                            stats["drop_missing_fields"] += 1
                            continue
                        # Schema tekrar
                        try:
                            validate(feat, ex["output"])
                        except SchemaError:
                            stats["drop_schema"] += 1
                            continue
                        inp_text = _normalize(_input_text(ex))

                    # Eval exact
                    if inp_text in eval_inputs[feat]:
                        stats["drop_eval_exact"] += 1
                        continue

                    # Eval cosine
                    dummy = Dedup(threshold=args.eval_threshold)
                    dummy.load_existing(list(eval_inputs[feat]))
                    if not dummy.should_add(inp_text):
                        stats["drop_eval_cosine"] += 1
                        continue

                    # Kendi içinde dedup
                    if not train_dedup.should_add(inp_text):
                        stats["drop_dedup"] += 1
                        continue

                    # PII güvencesi (kaynak zaten scrub'ladı ama paranoya)
                    ex = scrub_json_values(ex)

                    out_line = {
                        "feature": feat,
                        "source": source_name,
                        "messages": _to_messages(ex),
                    }
                    fout.write(json.dumps(out_line, ensure_ascii=False) + "\n")
                    stats["written"] += 1
                    stats["per_feature"][feat][source_name] += 1

    print(f"[merge] toplam yazılan: {stats['written']} -> {out_path}")
    print(f"[drop] eval-exact={stats['drop_eval_exact']}, eval-cosine={stats['drop_eval_cosine']}, "
          f"schema={stats['drop_schema']}, dedup={stats['drop_dedup']}, missing={stats['drop_missing_fields']}")
    print("[per-feature]")
    for feat, by_src in stats["per_feature"].items():
        print(f"  {feat}: {by_src}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
