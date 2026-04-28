# AI Fine-tune Eğitim Mantığı — gemma3:4b QLoRA

**Bağlam:** [`ai-agent-fine-tuning-plan.md`](./ai-agent-fine-tuning-plan.md), [`ai-agent-finetune-2026-04-25-progress.md`](./ai-agent-finetune-2026-04-25-progress.md)
**Tarih:** 2026-04-27

Bu doküman Faz 2 eğitim sürecinin **mantığını** ve **mühendislik kararlarını** açıklar. Savunma sırasında sorulabilecek "neden böyle yaptınız?" sorularına cevap niteliğindedir.

---

## 1. Büyük Resim — Ne Yapıyoruz?

**Hedef:** Google'ın açık kaynak `gemma3:4b` modelini, BitirmeProject domain'ine **özelleştirmek**.

**Sorun:** Base `gemma3:4b` genel amaçlı. Ona "issue zenginleştir" deyince JSON üretiyor ama bizim şemamızı tutturmuyor (smoke test: schema compliance %0). Format'ı doğru, ama "bizim formatımız" değil.

**Çözüm:** Modeli **bizim 837 örneğimiz üzerinde** ek olarak eğit. Sonuç: bizim şemamızı, bizim Türkçemizi, bizim agent davranışlarımızı öğrenmiş bir model.

---

## 2. Niçin Sıfırdan Eğitmiyoruz? — Transfer Learning

`gemma3:4b` modeli Google'ın milyarlarca dökümanla eğittiği bir taban (4.3 milyar parametre). Sıfırdan eğitmek **trilyonlarca dolar** + ay sürer + binlerce GPU ister.

Onun yerine: **zaten konuşmayı bilen modeli alıp, sadece bizim alanımıza adapte ediyoruz**. Sürücü ehliyeti olan birine "bizim şirket aracını kullanmayı" öğretmek gibi — sıfırdan sürmeyi öğretmiyorsun.

---

## 3. QLoRA — Niye Tüm Modeli Değil de "Adapter" Eğitiyoruz?

Modelin tamamını eğitsen 4.3 milyar parametre × 4 byte (fp32) = **16 GB ağırlık** + gradient'ler + optimizer state = ~80 GB VRAM. T4'ün 16 GB'ı ile imkansız.

**LoRA (Low-Rank Adaptation) hilesi:**
- Modelin tüm ağırlıklarını **donduruyoruz** (eğitilmiyor)
- Yanına küçük "adapter" matrisleri ekliyoruz — sadece bunlar eğitiliyor
- Çıktı: `Trainable parameters = 5.9M (%0.14)` — orijinalin binde 1.4'ü

**QLoRA bonus:** Donmuş 4.3B parametreyi 4-bit'e quantize ediyoruz (`load_in_4bit: true`). 16 GB → 4 GB'a iniyor. T4'e rahat sığıyor.

**Sonuç:** Adapter dosyası ~80 MB. Inference'ta base model + adapter birleştirilir, sanki tüm model fine-tune edilmiş gibi davranır.

---

## 4. Dataset — Neyi Öğretiyoruz?

`train-v1.train.jsonl` içinde 837 örnek var. Her örnek 3 mesajlık bir konuşma:

```json
{
  "messages": [
    {"role": "system", "content": "Sen BitirmeProject AI agent'ısın. Yalnızca geçerli JSON döndürürsün..."},
    {"role": "user", "content": "{\"title\": \"Kullanıcı şifre sıfırlama\"}"},
    {"role": "assistant", "content": "{\"description\": \"...\", \"acceptanceCriteria\": [...], ...}"}
  ]
}
```

**3 feature öğretiyoruz:**
1. **scaffold-project** — kullanıcı isteğinden proje + sprint + issue planı üret
2. **enrich-issue** — issue başlığından detay üret (description, AC, edge case'ler)
3. **generate-plan** — projeye yeni özellik için sprint planı üret

Model her örnekte: "user mesajını gör → assistant gibi cevap ver" mantığını içselleştiriyor.

---

## 5. Eğitim Mekaniği — Loss Nasıl Düşüyor?

**Bir step'te ne oluyor?**

1. Dataset'ten 1 örnek alınır (`per_device_train_batch_size: 1`)
2. Model `system + user` kısmına bakıp, `assistant` cevabını **tahmin** eder (token-by-token)
3. Tahmin ile gerçek cevap karşılaştırılır → **loss** hesaplanır (cross-entropy)
4. Loss'un gradient'i hesaplanır — "hangi adapter parametresini ne kadar değiştirsem cevap daha iyi olurdu?"
5. **Henüz parametreyi güncellemez** — 16 örnek biriktirir (`gradient_accumulation_steps: 16`)
6. 16 örnek sonra ortalama gradient ile parametre tek seferde güncellenir → **bir step tamamlanır**

**Effective batch size = 1 × 16 = 16** — yani her step aslında 16 örnekten öğreniyor, sadece RAM dar olduğu için tek tek işliyoruz.

**159 step × 16 örnek = 2544 toplam örnek görür** — 3 epoch × 837 = 2511 (yaklaşık eşit).

**Loss yorumlama:**
- Step 25: 1.32 → "şu an cevapları %35 olasılıkla doğru tahmin ediyor"
- Step 50: 1.11 → "%45 olasılıkla doğru"
- Step 159 hedef: 0.5-0.8 → "%60-70 olasılıkla doğru"

(Tam dönüşüm değil ama mantığı bu — düşük loss = daha güvenli tahmin)

---

## 6. Optimizer ve Learning Rate — Neden 2e-4?

**Optimizer:** `adamw_8bit` — gradient'i kullanıp parametreyi günceller. "8bit" optimizer state'i 8-bit'te tutar (yine bellek hilesi).

**Learning rate:** `2e-4` (0.0002) — her step'te parametreyi **ne kadar** değiştireceğin.
- Çok büyük → unstable, NaN, model bozulur
- Çok küçük → öğrenmez, loss düşmez
- 2e-4 → LoRA için "altın oran" (Unsloth/HuggingFace topluluk standardı)

**LR scheduler:** `cosine` — ilk %5'te yavaş yavaş 0'dan 2e-4'e çıkar (warmup), sonra cosine eğrisiyle yavaş yavaş 0'a iner. Eğitim sonuna doğru daha küçük adımlarla "ince ayar" yapar.

---

## 7. Validation — Neden Ayrı Set?

930 örneği 837/93'e böldük. **93 örnek validation** — model bunları **görmez**, eğitimde kullanılmaz.

Her 25 step'te validation loss ölçülür:
- **Train loss düşük + val loss düşük** → ✅ gerçekten öğreniyor
- **Train loss düşük + val loss yüksek** → ❌ overfit (sadece ezberliyor, yeni veriye genelleştiremiyor)

Şu an: train 1.11, val 1.19 → fark sadece 0.08 → **sağlıklı**.

---

## 8. Checkpoint — Neden Drive'a?

Eğitim 1.5 saat. Colab disconnect ederse RAM uçar — ama Drive'daki checkpoint kalır.

`save_steps: 25` → her 25 step'te:
- Adapter ağırlıkları (~80 MB)
- Optimizer state (~150 MB)
- LR scheduler state
- Random seed durumu
- Tokenizer

Hepsi `checkpoint-25/` klasörüne yazılır. `resume_from_checkpoint=last_ckpt` ile **bittiği yerden tam olarak** devam eder — sanki hiç durmamış gibi.

---

## 9. Quick Eval (Hücre 7) — Neden Eğitim İçinde?

Eğitim biter bitmez 5 örnek × 3 feature ile **sanity check** yapar:
- "JSON üretebiliyor mu?"
- "Mantıklı görünüyor mu?"

Bu ciddi eval değil — sadece "model çöplük olmadı mı?" kontrolü. Asıl eval lokalde, base ile **karşılaştırmalı** yapılacak (`tools/ai_eval/runner.py`).

---

## 10. Export (Hücre 8) — Neden GGUF?

Eğitim bitince adapter HuggingFace formatında (`.safetensors`). Ama biz Ollama kullanıyoruz, Ollama llama.cpp tabanlı, **GGUF format** ister.

Hücre 8 iki şey yapar:
1. **LoRA adapter'ı `.safetensors` olarak kaydet** (Drive'da)
2. **Adapter'ı GGUF q4_k_m formatına çevir** (~2.5 GB)
   - q4_k_m = 4-bit quantization, "k_m" varyantı (kalite/boyut dengesi)
   - Sonra Ollama Modelfile'ı bu GGUF'u `ADAPTER` olarak kullanır

---

## 11. Sonuç — Sonra Ne Olacak?

Eğitim bitince:
1. Drive'da `gemma3-4b-bp-v1-q4_k_m.gguf` (~2.5 GB) hazır
2. Lokale indirilir, `docker/ollama/` altına kopyalanır
3. `Modelfile`'da `ADAPTER` satırı aktif edilir, `ollama create bp-agent` çalıştırılır
4. Karşılaştırmalı eval: base `gemma3:4b` vs fine-tuned `bp-agent`
5. **Schema compliance:** %0 → %85+ hedef
6. **Format compliance:** %100 → %100 (zaten iyiydi)
7. **Field accuracy:** %50 → %75+ hedef

**Bitirme savunması için kanıt:** "Açık kaynak modeli alıp 837 örnekle 1.5 saatte özelleştirdik. Schema compliance 0'dan 85'e çıktı." → demo + grafik + karşılaştırma tablosu.

---

## Tek Cümlelik Özet

837 örnekle, gemma3:4b'nin %0.14'ünü (LoRA adapter) 159 step boyunca, 4-bit quantize edilmiş base üzerinde Adam optimizer + cosine LR ile eğitiyoruz; her 25 step'te Drive'a checkpoint atıyoruz; sonunda ~80 MB adapter çıkıyor, GGUF'a çeviriyoruz, Ollama'da `bp-agent` olarak kullanacağız.

---

## Hyperparametre Tablosu — Kararların Özeti

| Parametre | Değer | Neden? |
|---|---|---|
| `base_model` | unsloth/gemma-3-4b-it-bnb-4bit | 4-bit quantize edilmiş, T4'e sığar |
| `max_seq_length` | 1024 | Token p95=1164, bellek için 1024 yeterli |
| `lora.r` | 8 | Bellek dengesi (16 OOM verdi) |
| `lora.alpha` | 16 | r×2 (Unsloth standardı) |
| `target_modules` | q,k,v,o (sadece attention) | gate/up/down çıkarılınca bellek %30 düştü |
| `batch_size` | 1 | T4 fp32'ye düşünce zorunluluk |
| `grad_accum` | 16 | Effective batch=16 hedeflendi |
| `learning_rate` | 2e-4 | LoRA için topluluk standardı |
| `lr_scheduler` | cosine | Smooth eğitim eğrisi |
| `warmup_ratio` | 0.05 | İlk 8 step'te yavaş başla |
| `epochs` | 3 | 837 örnek için tipik (1=underfit, 5+=overfit) |
| `save_steps` | 25 | Disconnect riskine karşı sık checkpoint |
| `optim` | adamw_8bit | Bellek tasarrufu (state 8-bit) |
| `fp16` | true | T4 bf16 desteklemiyor (Unsloth otomatik fp32'ye düşebilir) |

---

## OOM Macerası — Ne Öğrendik?

İlk denemede `r=16`, full target_modules, `seq_len=2048` ile **CUDA OOM** aldık. Çözüm:
1. `r=16 → r=8` (adapter parametre sayısı yarıya)
2. `target_modules`: 7 modül → 4 modül (sadece attention)
3. `seq_len=2048 → 1024` (token p95=1164 olduğu için güvenli)
4. `batch_size=2 → 1`, `grad_accum=8 → 16` (effective batch korundu)

**Trade-off:** r=8 + sadece attention, model kapasitesini kısar — ama 837 örnek için yeterli kapasite. Daha büyük dataset ile r=16 + tüm modüller daha iyi sonuç verir, fakat bunun için A100 (40 GB) lazım.

---

## Idle/Disconnect Stratejisi

Colab Free 90 dk hareketsizlikte session'ı keser. Karşı tedbirler:
- **Tarayıcı console keep-alive:** Her 60 sn'de Connect butonuna click simulasyonu
- **Bilgisayar uyku kapalı:** Settings → Power → Sleep: Never
- **Drive checkpoint:** En kötü kayıp 25 step (~12-15 dk)

Disconnect olursa: Notebook açılır → Hücre 1-5 sırayla çalıştırılır → Hücre 6 otomatik son checkpoint'i bulur (`resume_from_checkpoint=last_ckpt`) → eğitim kaldığı yerden devam eder.
