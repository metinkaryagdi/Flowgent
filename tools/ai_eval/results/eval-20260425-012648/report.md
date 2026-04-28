# AI Agent Eval v1 — Rapor

**Tarih:** 2026-04-25 01:27
**Dataset:** `tests/AiEvalDataset/v1/` (60 örnek: 3 feature × 20)

## Özet

| Model | Feature | N | Format % | Schema % | Field Acc % | p50 ms | p95 ms |
|---|---|---:|---:|---:|---:|---:|---:|
| gemma3:4b | enrich-issue | 2 | 100.0 | 0.0 | 50.0 | 14216 | 14216 |

## Hedefler (plan dökümanından)

- Format compliance ≥ **95%**
- Schema compliance ≥ **90%**
- Field accuracy ≥ **80%**
- Latency p95 base'e göre ≤ **%20 yavaşlama**

## Notlar

- Her model için aynı system prompt + örnek input kullanıldı.
- Ollama `format=json`, `temperature=0.2`, `top_p=0.9`.
- Ham yanıtlar `raw-*.jsonl` dosyalarında — savunma sunumunda örnek gösterilebilir.
