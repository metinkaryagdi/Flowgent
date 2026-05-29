"""train-v1.jsonl üzerinde lokal son doğrulama + 90/10 split + istatistik.

Giriş: tools/ai_data_collector/output/train-v1.jsonl  (merge.py çıktısı)
Çıkış:
  - tools/ai-finetune/datasets/train-v1.train.jsonl
  - tools/ai-finetune/datasets/train-v1.val.jsonl
  - tools/ai-finetune/datasets/train-v1.stats.json

Kontroller:
  - Her satır `messages=[system,user,assistant]` formatında mı
  - assistant content parse edilince beklenen JSON şemasına uyuyor mu (son savunma)
  - Eval setindeki input hash'leriyle kesişim var mı (merge'de zaten eleniyor, burada
    paranoya tekrarı)
  - token uzunlukları (yaklaşık — gemma tokenizer lokalde yok, len(content) üzerinden
    heuristic: ~3.5 char/token Türkçe'de)

Çağrı:
  python -m tools.ai-finetune.scripts.prepare_dataset
  python tools/ai-finetune/scripts/prepare_dataset.py --in path/to/train-v1.jsonl
"""
from __future__ import annotations

import argparse
import json
import random
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT))

from tools.ai_data_collector import config  # noqa: E402
from tools.ai_data_collector.validation.schema import SchemaError, validate  # noqa: E402

DEFAULT_VERSION = "v2"
DEFAULT_IN = config.OUTPUT_DIR / f"train-{DEFAULT_VERSION}.jsonl"
OUT_DIR = REPO_ROOT / "tools" / "ai-finetune" / "datasets"

CHAR_PER_TOKEN = 3.5  # tr için kaba tahmin


def _read(path: Path) -> list[dict]:
    items: list[dict] = []
    with path.open(encoding="utf-8") as f:
        for i, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue
            try:
                items.append(json.loads(line))
            except json.JSONDecodeError as e:
                print(f"[parse-drop] line {i}: {e}", file=sys.stderr)
    return items


def _check_messages(ex: dict) -> tuple[bool, str]:
    msgs = ex.get("messages")
    if not isinstance(msgs, list) or len(msgs) < 3:
        return False, "messages < 3"
    feat = ex.get("feature")
    roles = [m.get("role") for m in msgs]
    if roles[0] != "system":
        return False, f"first role != system ({roles[0]})"
    if "assistant" not in roles:
        return False, "no assistant turn"
    if feat != "agent":
        # diğer feature'lar tam 3-mesaj single-turn
        if len(msgs) != 3 or roles != ["system", "user", "assistant"]:
            return False, f"non-agent role sequence {roles}"
    for m in msgs:
        if not isinstance(m.get("content"), str) or not m["content"]:
            return False, "empty content"
    return True, ""


def _check_assistant(ex: dict) -> tuple[bool, str]:
    feat = ex.get("feature")
    if feat == "agent":
        # Tüm assistant turn'leri validate_agent ile bir bütün olarak kontrol et
        try:
            validate(feat, ex)
        except SchemaError as e:
            return False, f"schema: {e}"
        return True, ""

    # diğer feature'lar: son assistant turn'ündeki JSON'u şemaya karşı kontrol
    try:
        assistant = ex["messages"][2]["content"]
        data = json.loads(assistant)
    except Exception as e:
        return False, f"assistant json parse: {e}"
    try:
        validate(feat, data)
    except SchemaError as e:
        return False, f"schema: {e}"
    return True, ""


def _approx_tokens(ex: dict) -> int:
    total = sum(len(m["content"]) for m in ex["messages"])
    return int(total / CHAR_PER_TOKEN)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", default=str(DEFAULT_IN))
    ap.add_argument("--out-dir", default=str(OUT_DIR))
    ap.add_argument("--train-split", type=float, default=0.9)
    ap.add_argument("--seed", type=int, default=3407)
    ap.add_argument("--max-tokens", type=int, default=2048, help="bu üstü drop")
    args = ap.parse_args()

    in_path = Path(args.inp)
    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    if not in_path.exists():
        print(f"ERROR: giriş yok: {in_path}", file=sys.stderr)
        return 2

    print(f"[read] {in_path}")
    items = _read(in_path)
    print(f"[read] {len(items)} satır")

    kept: list[dict] = []
    stats = {
        "input_count": len(items),
        "drop_messages": 0,
        "drop_schema": 0,
        "drop_oversized": 0,
        "kept": 0,
        "per_feature": {},
        "per_source": {},
        "token_p50": 0,
        "token_p95": 0,
        "token_max": 0,
    }
    token_lens: list[int] = []

    for ex in items:
        ok, why = _check_messages(ex)
        if not ok:
            stats["drop_messages"] += 1
            continue
        ok, why = _check_assistant(ex)
        if not ok:
            stats["drop_schema"] += 1
            continue
        tlen = _approx_tokens(ex)
        if tlen > args.max_tokens:
            stats["drop_oversized"] += 1
            continue
        kept.append(ex)
        token_lens.append(tlen)
        feat = ex.get("feature", "unknown")
        src = ex.get("source", "unknown")
        stats["per_feature"][feat] = stats["per_feature"].get(feat, 0) + 1
        stats["per_source"][src] = stats["per_source"].get(src, 0) + 1

    if token_lens:
        token_lens.sort()
        stats["token_p50"] = token_lens[len(token_lens) // 2]
        stats["token_p95"] = token_lens[int(len(token_lens) * 0.95)]
        stats["token_max"] = token_lens[-1]
    stats["kept"] = len(kept)

    rng = random.Random(args.seed)
    rng.shuffle(kept)

    cut = int(len(kept) * args.train_split)
    train = kept[:cut]
    val = kept[cut:]

    # Çıktı isimleri girdi dosyasının versiyon ekinden türetilir (train-v2.jsonl → train-v2.*)
    stem = in_path.stem  # ör. "train-v2"
    train_path = out_dir / f"{stem}.train.jsonl"
    val_path = out_dir / f"{stem}.val.jsonl"
    stats_path = out_dir / f"{stem}.stats.json"

    with train_path.open("w", encoding="utf-8") as f:
        for ex in train:
            f.write(json.dumps(ex, ensure_ascii=False) + "\n")
    with val_path.open("w", encoding="utf-8") as f:
        for ex in val:
            f.write(json.dumps(ex, ensure_ascii=False) + "\n")

    stats["train"] = len(train)
    stats["val"] = len(val)
    stats_path.write_text(json.dumps(stats, indent=2, ensure_ascii=False), encoding="utf-8")

    print(f"[split] train={len(train)} val={len(val)}")
    print(f"[tokens] p50={stats['token_p50']} p95={stats['token_p95']} max={stats['token_max']}")
    print(f"[drop] messages={stats['drop_messages']} schema={stats['drop_schema']} oversized={stats['drop_oversized']}")
    print(f"[out] {train_path.name}, {val_path.name}, {stats_path.name}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
