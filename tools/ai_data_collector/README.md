# AI Data Collector (Faz 1)

Fine-tune dataset'i üreten script'ler. Plan: `docs/ai-agent-fine-tuning-plan.md` Faz 1.

**Çıktı:** `tools/ai_data_collector/output/*.jsonl` (git-ignored).
**Hedef boyut:** ~1500 örnek.
**Dağılım (karar 2026-04-23):** %70 sentetik, %20 sessions, %10 manuel golden.

## Klasör yapısı

```
tools/ai_data_collector/
├── README.md                    # bu dosya
├── requirements.txt             # python deps
├── config.py                    # .env loader + sabitler
├── prompts/
│   ├── scaffold_project.py      # 8 prompt template varyantı
│   ├── enrich_issue.py          # 8 template
│   └── generate_plan.py         # 8 template
├── domains.py                   # 50+ domain × context varyasyonu
├── providers/
│   ├── groq_client.py           # primary: llama-3.3-70b
│   └── ollama_client.py         # fallback: qwen2.5:7b
├── validation/
│   ├── schema.py                # JSON schema check per feature
│   ├── pii.py                   # email/kullanıcı adı scrub
│   └── dedup.py                 # cosine similarity > 0.9 → eleme
├── synthetic_gen.py             # ana sentetik üretim entry point
├── collect_sessions.py          # AiSessions → PII-scrub → JSONL
├── merge.py                     # sentetik + sessions + golden → train-v1.jsonl
├── golden/                      # manuel hand-crafted (10% of final)
│   ├── scaffold-project.jsonl   # boş başla, Faz 1 sonunda doldur
│   ├── enrich-issue.jsonl
│   └── generate-plan.jsonl
└── output/                      # üretilen dosyalar (git-ignored)
    ├── synthetic-scaffold.jsonl
    ├── synthetic-enrich.jsonl
    ├── synthetic-plan.jsonl
    ├── sessions-*.jsonl
    └── train-v1.jsonl           # son birleştirilmiş dataset
```

## Kullanım sırası (Faz 1 akışı)

```bash
# 1. Kur
cd tools/ai_data_collector
pip install -r requirements.txt

# 2. Smoke test — 10 örnek, tek domain, tek feature
python -m tools.ai_data_collector.synthetic_gen --feature scaffold-project --count 10 --domain ecommerce --smoke

# 3. Tam sentetik üretim (gece bırakılır, ~2-3 saat)
python -m tools.ai_data_collector.synthetic_gen --feature scaffold-project --count 350
python -m tools.ai_data_collector.synthetic_gen --feature enrich-issue     --count 350
python -m tools.ai_data_collector.synthetic_gen --feature generate-plan    --count 350

# 4. Sessions'tan çekim (opsiyonel, sadece backend Docker ayaktaysa)
python -m tools.ai_data_collector.collect_sessions --limit 300

# 5. Manuel golden'ı doldur (hand-crafted, elle)
#    golden/*.jsonl dosyalarına 30-35 örnek/feature yaz

# 6. Birleştir
python -m tools.ai_data_collector.merge --out output/train-v1.jsonl

# 7. Drive'a upload (Colab için)
#    output/train-v1.jsonl → MyDrive/bp-finetune/datasets/
```

## Eval seti ile çakışma kontrolü

`merge.py` otomatik kontrol yapar:
- `tests/AiEvalDataset/v1/*.jsonl` içindeki her `input` hash'i output'ta varsa eler.
- Hash eşleşmesi dışında cosine similarity > 0.95 de eler.

## Sağlayıcı (provider) seçimi

- **Groq Free Tier** (`llama-3.3-70b-versatile`) — dakikada ~30 istek, ana üretici.
- **Ollama** (`qwen2.5:7b`, CPU) — Groq rate-limit / 429 / timeout fallback.

`synthetic_gen.py` her örnek için önce Groq dener, 429 / exception aldığında Ollama'ya düşer. Üretim sonrası her örnek **schema validation + PII scrub + dedup** üçlüsünden geçer; geçemeyen atılır.
