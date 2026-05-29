# v2 Colab Fine-tune Bundle

Bu klasör, **Drive'a tek seferde upload edip** Colab'da fine-tune yapmak için gerekli her şeyi içeriyor.

## Klasör içeriği

```
v2-drive-bundle/
├── README.md                          # bu dosya
├── train_v2.ipynb                     # Colab notebook (T4 için optimize)
├── datasets/
│   ├── train-v2.train.jsonl           # 1275 örnek, 3.9 MB
│   ├── train-v2.val.jsonl             # 142 örnek, 440 KB
│   └── train-v2.stats.json            # feature dağılımı
├── configs/
│   └── v2.yaml                        # LoRA r=32, 3 epoch, T4 için ayar
└── eval/
    └── samples.jsonl                  # 5 sanity test prompt'u (opsiyonel)
```

## Adım adım — sıfırdan training'e

### 1. Drive'a upload (~5 dk)

1. Google Drive'da yeni klasör aç: `MyDrive/bp-finetune-v2/`
2. Bu `v2-drive-bundle/` klasörünün **içeriğini** o klasöre kopyala. Sonuç şu yapı olmalı:
   ```
   MyDrive/bp-finetune-v2/
   ├── train_v2.ipynb
   ├── datasets/
   │   ├── train-v2.train.jsonl
   │   ├── train-v2.val.jsonl
   │   └── train-v2.stats.json
   ├── configs/
   │   └── v2.yaml
   └── eval/
       └── samples.jsonl
   ```

   **Önemli:** Notebook `MyDrive/bp-finetune-v2/datasets/...` yolunu bekliyor. Farklı yere koyarsan cell 2'de hata verir.

### 2. Colab'da notebook'u aç

1. Drive'da `train_v2.ipynb`'ye sağ tık → **Open with → Google Colaboratory**
2. **Runtime → Change runtime type → T4 GPU** seç → Save
3. **Runtime → Run all** (veya cell'leri tek tek çalıştır)

### 3. Cell'ler ne yapar

| Cell | İş | Süre | Hata olursa |
|---|---|---|---|
| 1 — GPU check | `nvidia-smi` ile T4'ü doğrula | 5 sn | Runtime type T4 olmalı |
| 2 — Drive mount | Dataset varlığını kontrol et | 30 sn | Dosya yolları yukarıdaki yapıyla aynı mı? |
| 3 — Bağımlılıklar | Pinned versiyonlar (unsloth, peft, trl, vs.) | 3-5 dk | `%%capture` çıktıyı gizler, hata varsa cell'i kaldır ve tekrar çalıştır |
| 4 — Version check | İmport doğrulama | 5 sn | Mismatch → cell 3'ü tekrar çalıştır |
| 5 — Model + LoRA | Gemma3-4B 4-bit + LoRA r=32 | 1-2 dk | "no GPU" → Runtime type kontrol |
| 6 — Dataset | Chat template uygula | 30 sn | — |
| 7 — Eğitim | 240 step (~3.5 saat T4'te) | **uzun** | Disconnect olursa: cell 1-5 tekrar → cell 7 → otomatik resume |
| 8 — Save adapter | Drive'a yaz (~50 MB) | 30 sn | — |
| 9 — Sanity test | 5 prompt → JSON parse | 1 dk | — |

### 4. Eğitim sırasında

- **Tarayıcı sekmesini açık tut** — Colab 90 dk idle'da disconnect olur.
- **Mobil internet OK** — eğitim cloud'da, senin tarafından ~150 MB toplam.
- **Loss curve** — cell 7 stdout'unda her 10 step'te basılır. Sağlıklı görünüm: ~1.5 → 0.4 civarı.
- **Disconnect olursa:**
  1. Notebook'u baştan aç
  2. Runtime → Connect (eğer "Reconnect" gözüküyorsa onu seç)
  3. Cell 1 → 2 → 3 → 4 → 5 → 6 → 7 sırayla. **Cell 7 otomatik son checkpoint'i bulup oradan devam eder.**
  4. Checkpoint'ler `MyDrive/bp-finetune-v2/checkpoints-v2/checkpoint-50, -100, -150 ...` altında

### 5. Eğitim bittikten sonra (yerelde)

LoRA adapter Drive'da `MyDrive/bp-finetune-v2/adapters/gemma3-4b-bp-v2/`.

Memory'deki [project_gemma3_export_pipeline] pipeline'ını izle:

```powershell
# 1. Drive'dan adapter'ı yerele indir
# (Google Drive web UI'dan tek klasör olarak indir)

# 2. Yerelde gemma-export ortamına gel
cd C:\Users\Metin\Desktop\gemma-export
.\.venv\Scripts\activate

# 3. LoRA → fp16 merge (manual_merge.py — language_model only)
python manual_merge.py `
  --base unsloth/gemma-3-4b-it-bnb-4bit `
  --adapter ./gemma3-4b-bp-v2 `
  --out ./merged-v2

# 4. fp16 → GGUF
python llama.cpp\convert_hf_to_gguf.py ./merged-v2 --outfile gemma3-4b-bp-v2.f16.gguf

# 5. f16 → Q4_K_M (Ollama uyumlu)
.\llama.cpp\build\bin\Release\llama-quantize.exe gemma3-4b-bp-v2.f16.gguf gemma3-4b-bp-v2.q4_k_m.gguf Q4_K_M

# 6. Ollama'ya yükle
docker cp gemma3-4b-bp-v2.q4_k_m.gguf <none>   # NOT: host Ollama kullanıyorsan direkt
ollama create bp-agent-v2 -f Modelfile

# 7. Backend'i fine-tune'a geçir
# .env: OLLAMA_USE_FINETUNED=true, OLLAMA_FINETUNED_MODEL=bp-agent-v2
docker compose up -d --force-recreate ai-api
```

## Geçen sefer (v1) yaşanan hatalar ve v2'de çözümleri

| v1 Sorunu | v2 Çözümü |
|---|---|
| Eğitim 125 step'te kesildi (disconnect) | `save_steps=50` (v1: 100) + `save_total_limit=10` (v1: 3) — daha sık kaydet, daha fazla geri dön |
| Resume çalışmadı | Cell 7'de otomatik checkpoint dir taraması — her zaman son checkpoint'ten devam |
| 0 tool-calling örneği — agent skill yok | **487 agent örneği** (yeni); multi-turn `[tool]` formatı dahil |
| LoRA r=8/16 yetersiz | r=32 + alpha=64 (kapasite 2×) |
| `save_pretrained_gguf` Gemma3 multimodal'da kırık | **GGUF export notebook'tan kaldırıldı** — yerel `manual_merge.py` pipeline'ı kullan |
| Colab RAM 12 GB aşımı | Notebook sadece LoRA adapter kaydediyor (~50 MB), merge/GGUF yerelde |
| `torch~=2.6.0` çakışması | `--no-deps` flag'leriyle torch ve diğer kritik paketler pin'li |
| Inference format mismatch | AgentLoop chat API'ye geçti (`/api/chat` + messages list) — backend kısmı zaten yapıldı |

## Beklenen süre

- Drive upload: 5 dk
- Bağımlılık install: 5 dk
- Eğitim: **3-3.5 saat** (240 step × ~50s)
- Adapter save + sanity test: 5 dk
- **Toplam Colab oturumu: ~4 saat**

Yerel GGUF export adımı ayrı (1-2 saat).

## Sorun giderme

**"GPU bulunamadı" hatası**  
Runtime → Change runtime type → T4 GPU seçili olmalı. Free tier'da T4 her zaman müsait olmayabilir; 5-10 dk sonra tekrar dene.

**Bağımlılık install sırasında "ERROR: cannot install ..." veya "torch CUDA mismatch"**  
Cell 3'ü tekrar çalıştır (`%%capture` siler). Sırayla çalıştırdığından emin ol — Unsloth ilk, sonra peft/trl/accelerate.

**"OutOfMemoryError" — VRAM yetmedi**  
v2.yaml içinde `per_device_train_batch_size: 2 → 1`, `gradient_accumulation_steps: 8 → 16` yap. Effective batch 16 sabit kalır, daha az VRAM kullanır.

**Sanity test'te tüm prompt'lar FAIL**  
Loss curve'a bak (cell 7 çıktısı). Loss > 1.0 kaldıysa eğitim yeterli ilerlememiş — epoch sayısını v2.yaml'da 3 → 5'e çıkar, baştan başla.

**Adapter klasörü Drive'da yok**  
Cell 8 çalıştı mı? Manuel olarak `model.save_pretrained('/content/drive/MyDrive/bp-finetune-v2/adapters/gemma3-4b-bp-v2')` çalıştır.

## Bu bundle'ı tekrar üretmek

Repo'da değişiklik olduğunda:

```bash
# Repo root'tan
python -m tools.ai_data_collector.agent_synth --count 500 --seed 42
python -m tools.ai_data_collector.merge --out tools/ai_data_collector/output/train-v2.jsonl
python -m tools.ai-finetune.scripts.prepare_dataset

# Bundle'ı yenile
cp tools/ai-finetune/datasets/train-v2.train.jsonl tools/ai-finetune/v2-drive-bundle/datasets/
cp tools/ai-finetune/datasets/train-v2.val.jsonl tools/ai-finetune/v2-drive-bundle/datasets/
cp tools/ai-finetune/datasets/train-v2.stats.json tools/ai-finetune/v2-drive-bundle/datasets/
cp tools/ai-finetune/configs/v2.yaml tools/ai-finetune/v2-drive-bundle/configs/
```
