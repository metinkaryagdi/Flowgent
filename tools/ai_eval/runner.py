"""Eval runner — base ve fine-tuned modelleri tests/AiEvalDataset/v1/ üzerinde çalıştırır.

Çıktı:
  tools/ai_eval/results/eval-<timestamp>/
    raw-<model>-<feature>.jsonl    # her örnek için ham yanıt + metrikler
    summary.json                   # model × feature istatistik tablosu
    report.md                      # insan okuyabilir rapor (savunma için)

Kullanım:
  # Base karşılaştırması
  python -m tools.ai_eval.runner --models gemma3:4b,bp-agent

  # Sadece base (ft yok henüz)
  python -m tools.ai_eval.runner --models gemma3:4b

  # Hızlı deneme
  python -m tools.ai_eval.runner --models gemma3:4b --limit 3

Ön-koşul: Ollama ayakta + modeller pull'lanmış.
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from datetime import datetime
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO_ROOT))

from tools.ai_eval.metrics import (  # noqa: E402
    AggregateStats,
    SampleResult,
    evaluate_fields,
    evaluate_format,
    evaluate_schema,
)
from tools.ai_eval.providers import OllamaClient, ProviderError  # noqa: E402

EVAL_DIR = REPO_ROOT / "tests" / "AiEvalDataset" / "v1"
OUT_ROOT = REPO_ROOT / "tools" / "ai_eval" / "results"

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
}

FEATURES = tuple(SYSTEM_PROMPTS.keys())


def _read_eval(feature: str) -> list[dict]:
    path = EVAL_DIR / f"{feature}.jsonl"
    out: list[dict] = []
    with path.open(encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if line:
                out.append(json.loads(line))
    return out


def _run_sample(client: OllamaClient, feature: str, ex: dict) -> SampleResult:
    system = SYSTEM_PROMPTS[feature]
    user = json.dumps(ex["input"], ensure_ascii=False)

    r = SampleResult(id=ex["id"], feature=feature, model=client.model, latency_ms=0.0)

    try:
        raw, dt = client.chat(system, user)
        r.raw_output = raw
        r.latency_ms = dt
    except ProviderError as e:
        r.error = f"provider: {e}"
        return r

    # Format
    ok, data, err = evaluate_format(raw)
    r.format_ok = ok
    if not ok:
        r.error = err
        return r
    r.parsed = data

    # Schema
    ok, err = evaluate_schema(feature, data)
    r.schema_ok = ok
    if not ok:
        r.error = err
        # schema fail olsa bile field accuracy'ı hesapla — tamamen boş rapor verme

    # Field accuracy
    score, mc_hit, mc_total, mnc_clean, mnc_total = evaluate_fields(
        data,
        ex.get("must_contain", []),
        ex.get("must_not_contain", []),
    )
    r.field_accuracy = score
    r.must_contain_hit = mc_hit
    r.must_contain_total = mc_total
    r.must_not_contain_clean = mnc_clean
    r.must_not_contain_total = mnc_total

    return r


def _write_raw(out_dir: Path, model: str, feature: str, results: list[SampleResult]) -> None:
    safe_model = model.replace(":", "_").replace("/", "_")
    path = out_dir / f"raw-{safe_model}-{feature}.jsonl"
    with path.open("w", encoding="utf-8") as f:
        for r in results:
            obj = {
                "id": r.id,
                "feature": r.feature,
                "model": r.model,
                "latency_ms": round(r.latency_ms, 1),
                "format_ok": r.format_ok,
                "schema_ok": r.schema_ok,
                "field_accuracy": round(r.field_accuracy, 3),
                "mc": f"{r.must_contain_hit}/{r.must_contain_total}",
                "mnc_clean": f"{r.must_not_contain_clean}/{r.must_not_contain_total}",
                "error": r.error,
                "raw_preview": r.raw_output[:300],
            }
            f.write(json.dumps(obj, ensure_ascii=False) + "\n")


def _build_report(summary: dict, out_dir: Path) -> None:
    """Markdown rapor — karşılaştırma tablosu + savunma özeti."""
    lines: list[str] = [
        "# AI Agent Eval v1 — Rapor",
        "",
        f"**Tarih:** {datetime.now().strftime('%Y-%m-%d %H:%M')}",
        f"**Dataset:** `tests/AiEvalDataset/v1/` (60 örnek: 3 feature × 20)",
        "",
        "## Özet",
        "",
        "| Model | Feature | N | Format % | Schema % | Field Acc % | p50 ms | p95 ms |",
        "|---|---|---:|---:|---:|---:|---:|---:|",
    ]

    for row in summary["rows"]:
        lines.append(
            f"| {row['model']} | {row['feature']} | {row['n']} | "
            f"{row['format_compliance_pct']} | {row['schema_compliance_pct']} | "
            f"{row['field_accuracy_pct']} | {int(row['latency_p50_ms'])} | "
            f"{int(row['latency_p95_ms'])} |"
        )

    lines += [
        "",
        "## Hedefler (plan dökümanından)",
        "",
        "- Format compliance ≥ **95%**",
        "- Schema compliance ≥ **90%**",
        "- Field accuracy ≥ **80%**",
        "- Latency p95 base'e göre ≤ **%20 yavaşlama**",
        "",
        "## Notlar",
        "",
        "- Her model için aynı system prompt + örnek input kullanıldı.",
        "- Ollama `format=json`, `temperature=0.2`, `top_p=0.9`.",
        "- Ham yanıtlar `raw-*.jsonl` dosyalarında — savunma sunumunda örnek gösterilebilir.",
        "",
    ]

    (out_dir / "report.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--models", required=True,
                    help="virgülle ayrılmış model listesi, örn: gemma3:4b,bp-agent")
    ap.add_argument("--features", default=",".join(FEATURES))
    ap.add_argument("--limit", type=int, default=None,
                    help="feature başına max örnek (quick run)")
    ap.add_argument("--base-url", default=None)
    ap.add_argument("--out", default=None, help="çıkış klasörü (varsayılan timestamp'li)")
    args = ap.parse_args()

    models = [m.strip() for m in args.models.split(",") if m.strip()]
    features = [f.strip() for f in args.features.split(",") if f.strip() in FEATURES]
    if not features:
        print(f"ERROR: geçerli feature yok. Kullanılabilir: {FEATURES}", file=sys.stderr)
        return 2

    stamp = args.out or datetime.now().strftime("eval-%Y%m%d-%H%M%S")
    out_dir = OUT_ROOT / stamp
    out_dir.mkdir(parents=True, exist_ok=True)

    # Ping kontrol
    probe = OllamaClient(model=models[0], base_url=args.base_url or "")
    if not probe.ping():
        print(f"ERROR: Ollama {probe.base_url} erişilemiyor.", file=sys.stderr)
        return 2

    # Her model için has_model kontrolü
    for m in models:
        c = OllamaClient(model=m, base_url=args.base_url or "")
        if not c.has_model():
            print(f"WARN: {m} Ollama'da yok — 'ollama pull {m}' veya Modelfile ile oluştur.", file=sys.stderr)

    summary = {
        "timestamp": stamp,
        "models": models,
        "features": features,
        "dataset_dir": str(EVAL_DIR.relative_to(REPO_ROOT)),
        "rows": [],
    }

    t_start = time.perf_counter()

    for model in models:
        client = OllamaClient(model=model, base_url=args.base_url or "")
        for feature in features:
            samples = _read_eval(feature)
            if args.limit:
                samples = samples[:args.limit]

            print(f"[{model}] {feature}: {len(samples)} örnek...")

            results: list[SampleResult] = []
            agg = AggregateStats(model=model, feature=feature)

            for i, ex in enumerate(samples, 1):
                r = _run_sample(client, feature, ex)
                results.append(r)
                agg.add(r)
                status = "ok" if r.format_ok and r.schema_ok else ("format?" if not r.format_ok else "schema?")
                print(f"  [{i}/{len(samples)}] {ex['id']}: {status} "
                      f"mc={r.must_contain_hit}/{r.must_contain_total} "
                      f"mnc={r.must_not_contain_clean}/{r.must_not_contain_total} "
                      f"lat={int(r.latency_ms)}ms")

            _write_raw(out_dir, model, feature, results)
            summary["rows"].append(agg.as_dict())

    summary["total_duration_s"] = round(time.perf_counter() - t_start, 1)

    (out_dir / "summary.json").write_text(
        json.dumps(summary, indent=2, ensure_ascii=False), encoding="utf-8"
    )
    _build_report(summary, out_dir)

    print(f"\n[done] {out_dir}")
    print(f"  summary.json, report.md, raw-*.jsonl")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
