# AI Eval Dataset v1

**Amaç:** Fine-tune öncesi ve sonrası modeli karşılaştırmak için sabit
eval seti. Bu dataset **training'e DAHİL EDİLMEZ** — leakage olursa
metrik anlamını yitirir.

**Kapsam:** 3 özellik (`scaffold-project`, `enrich-issue`, `generate-plan`).

**Durum (2026-04-24):** Her dosyada 10 starter örnek var. Faz 1 başında
her dosya 20'ye çıkarılacak (plan hedefi). Yeni örnekler eklenirken
domain çeşitliliği korunacak (aynı alandan 2'den fazla örnek yok).

## Format

Her satır tek bir JSON object. Alan şeması:

```json
{
  "id": "string, stable id (kebab-case, e.g. 'scaffold-ecom-01')",
  "feature": "scaffold-project | enrich-issue | generate-plan",
  "input": { /* özelliğin girdi şeması */ },
  "expected_json_shape": { /* beklenen çıktı yapısının referans örneği */ },
  "must_contain": ["tr keyword 1", "tr keyword 2"],
  "must_not_contain": ["english phrase", "markdown fence"]
}
```

## Alan anlamları

- **`id`**: stable reference for regression tracking.
- **`feature`**: evaluator hangi prompt/handler'a yönlendireceğini bilir.
- **`input`**: özelliğin production API'sine gönderilecek payload.
- **`expected_json_shape`**: modelin çıktısının bu objenin **yapısına**
  (alanlar + tip) uymasını bekliyoruz. Değerlerin birebir eşleşmesi
  gerekmez; `must_contain` içerik kontrolü için var.
- **`must_contain`**: model çıktısının case-insensitive olarak içermesi
  gereken substring'ler. Domain terminolojisi, zorunlu kavramlar.
- **`must_not_contain`**: model çıktısının içermemesi gereken
  substring'ler. Generic LLM gevezeliği, İngilizce sızıntısı, markdown.

## Evaluation metrikleri (Faz 3)

1. **Format compliance:** çıktı parse edilebilen JSON mu? (boolean)
2. **Schema compliance:** `expected_json_shape` ile alan/tip eşleşmesi (%)
3. **Field accuracy:** `must_contain` hit oranı × `must_not_contain` miss oranı
4. **Latency:** p50/p95

Hedefler plan dökümanında (`docs/ai-agent-fine-tuning-plan.md` Faz 3).

## Domain dağılımı (v1 starter)

Her dosyada 10 örnek, domain çeşitliliği:
- e-ticaret, eğitim, sağlık, fintech, oyun, devops, mobil app,
  kurumsal içi araç, SaaS B2B, IoT.

## Çakışma kontrolü

- Eval setindeki hiçbir örnek sentetik training setinde **birebir**
  bulunmamalı.
- Manuel golden set (`tools/ai-data-collector/golden/`) bu setle
  **ayrı tutulur** (plan kararı 2026-04-23).
