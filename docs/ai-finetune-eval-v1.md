# AI Agent Fine-tune — Eval Raporu v1

**Tarih:** [doldurulacak — Faz 2 eğitimi sonrası]
**Branch:** `feature/ai-agent-finetune` (veya merge edildi: commit hash)
**Plan:** [`ai-agent-fine-tuning-plan.md`](./ai-agent-fine-tuning-plan.md)
**Hedef davranışlar:** [`ai-agent-target-behaviors.md`](./ai-agent-target-behaviors.md)
**Adapter:** [link/yol — örn. `docker/ollama/gemma3-4b-bp-v1-q4_k_m.gguf` veya HF Hub]

---

## Özet

Bu rapor, base `gemma3:4b` ile fine-tuned `bp-agent` modelinin
[`tests/AiEvalDataset/v1/`](../tests/AiEvalDataset/v1/) (60 örnek: 3 feature × 20)
üzerindeki performansını karşılaştırır. **Bitirme savunmasında "fine-tune'un
ölçülebilir faydası" iddiasının kanıtıdır.**

---

## Metrikler ve hedefler

| Metrik | Hedef | Açıklama |
|---|---|---|
| Format compliance | ≥ 95% | Çıktı parse edilebilen JSON mu (markdown fence/açıklama tolere edilir) |
| Schema compliance | ≥ 90% | Alan/tip uyumu (`tools/ai_data_collector/validation/schema.py`) |
| Field accuracy | ≥ 80% | `must_contain` hit oranı × `must_not_contain` miss oranı ortalaması |
| Latency p95 | ≤ base + 20% | Fine-tune adapter overhead'i kabul edilebilir |

---

## Sonuçlar

### Genel karşılaştırma (3 feature toplam)

| Model | N | Format % | Schema % | Field Acc % | p50 ms | p95 ms |
|---|---:|---:|---:|---:|---:|---:|
| gemma3:4b (base) | 60 | _runner_ | _runner_ | _runner_ | _runner_ | _runner_ |
| bp-agent (fine-tuned) | 60 | _runner_ | _runner_ | _runner_ | _runner_ | _runner_ |
| **Δ (kazanım)** | — | _Δ_ | _Δ_ | _Δ_ | _Δ_ | _Δ_ |

> Tablolar `tools/ai_eval/runner.py --models gemma3:4b,bp-agent` çıktısından doldurulur.

### Feature başına kırılım

| Feature | Model | Format % | Schema % | Field Acc % | p50 ms |
|---|---|---:|---:|---:|---:|
| scaffold-project | gemma3:4b | | | | |
| scaffold-project | bp-agent  | | | | |
| enrich-issue     | gemma3:4b | | | | |
| enrich-issue     | bp-agent  | | | | |
| generate-plan    | gemma3:4b | | | | |
| generate-plan    | bp-agent  | | | | |

---

## Manuel kalite skoru (blind A/B)

Plan dökümanı 1-5 arası blind puanlama öneriyor. Hedef: fine-tuned ortalama base + 0.7.

| Hakem | Base ort. | FT ort. | Δ |
|---|---:|---:|---:|
| Sen | | | |
| 2. hakem | | | |

Yöntem: `tools/ai_eval/results/eval-<ts>/raw-*.jsonl` dosyalarından her feature için
5 örnek seçilir, model isimleri gizlenir, hakem yan yana 1-5 puanlar.

---

## Kabul kararı

| Otomatik kriter | Geçti? |
|---|---|
| Format ≥ %95 | ☐ |
| Schema ≥ %90 | ☐ |
| Field Acc ≥ %80 | ☐ |
| Latency p95 ≤ %20 yavaşlama | ☐ |
| Manuel skor Δ ≥ 0.7 | ☐ |

**Karar:**
- ☐ Tüm kriterler geçti → Faz 4 (deploy) açılır.
- ☐ Bazıları kaldı → veri kalitesini iyileştir, hyperparameter tarat (Colab quota'ya dikkat).
- ☐ Hiçbiri geçmedi → veri stratejisi yeniden gözden geçirilir; 1B yedek plan değerlendirilir.

---

## Örnekler (savunma için)

Aynı eval örneği için base ve fine-tuned çıktıları yan yana — format kayması ve
domain dilindeki fark anchor olur.

### Örnek 1 — `enrich-pwreset-02`

**Girdi:**
```json
{ "title": "Parola sıfırlama akışı", "projectContext": "web SaaS" }
```

**Base (`gemma3:4b`):**
```json
[base çıktı buraya — raw-gemma3_4b-enrich-issue.jsonl'dan]
```

**Fine-tuned (`bp-agent`):**
```json
[FT çıktı buraya — raw-bp-agent-enrich-issue.jsonl'dan]
```

**Gözlem:** [açıklama — örn. base array verdi, FT şemaya uydu]

### Örnek 2 — `scaffold-saas-09`

[aynı kalıp]

### Örnek 3 — `plan-saas-billing-09`

[aynı kalıp]

---

## Gelecek iş

- **v2 dataset:** `AiFailedResponses` tablosundan toplanır (Faz 6 feedback loop).
- **Tool-calling eval:** `tools/ai_eval/runner.py` şu an sadece tek-shot generation
  ölçüyor; agent loop'un başarısı (kaç iter, kaç tool, kaç fail) için ayrı runner
  yazılacak.
- **Daha geniş eval seti:** v1 60 örnek; v2'de 100+ ve domain dağılımı genişletilir.

---

## Komutlar (yeniden üretmek için)

```bash
# Eval çalıştır
python -m tools.ai_eval.runner --models gemma3:4b,bp-agent

# Çıktılar
ls tools/ai_eval/results/eval-<timestamp>/
# raw-gemma3_4b-*.jsonl, raw-bp-agent-*.jsonl, summary.json, report.md
```

Bu döküman `report.md` çıktısı ile birleştirilerek savunmaya götürülür.
