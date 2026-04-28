# ai-finetune — Faz 2 Eğitim Pipeline

**Amaç:** `gemma3:4b`'yi BitirmeProject domain'ine QLoRA ile fine-tune et, Ollama-uyumlu GGUF çıkart.

## Klasörler

```
tools/ai-finetune/
├── configs/v1.yaml              # hyperparametre — Colab ve lokal ortak
├── scripts/
│   ├── prepare_dataset.py       # merge çıktısını 90/10 split + şema doğrulama
│   └── export_to_gguf.py        # post-hoc lokal LoRA→GGUF (Colab'da gerek yok)
├── colab/train.ipynb            # Colab T4 eğitim notebook'u (9 code cell)
├── datasets/                    # prepare_dataset çıktıları (git-ignored)
├── adapters/                    # final adapter + gguf (git-ignored)
└── checkpoints/                 # training checkpoint'leri (git-ignored)
```

## Akış

### 1. Lokal — dataset hazırla

```bash
# merge.py train-v1.jsonl üretmiş olmalı (Faz 1 sonu)
python tools/ai-finetune/scripts/prepare_dataset.py
```

Çıktılar:
- `datasets/train-v1.train.jsonl` (~%90)
- `datasets/train-v1.val.jsonl` (~%10)
- `datasets/train-v1.stats.json` — drop sayıları, token uzunluk p50/p95/max

### 2. Drive'a upload

```
MyDrive/bp-finetune/
  datasets/{train,val}.jsonl
  configs/v1.yaml              # tools/ai-finetune/configs/v1.yaml kopyası
  eval/v1/*.jsonl              # tests/AiEvalDataset/v1/ kopyası (quick eval için)
```

### 3. Colab

1. `colab/train.ipynb`'yi Colab'da aç
2. Runtime → T4 GPU
3. Hücreleri sırayla çalıştır
4. ~2-3 saat (3 epoch × ~1000 örnek)
5. `adapters/gemma3-4b-bp-v1/` Drive'da hazır olur (adapter + gguf + Modelfile)

### 4. Ollama'ya yükle (Faz 4)

```bash
# Lokal
ollama create bp-agent -f adapters/gemma3-4b-bp-v1/Modelfile
# Test
ollama run bp-agent "scaffold-project: Mahalle eczanesi için stok takip sistemi"
```

## Hyperparameter notları

`configs/v1.yaml` Colab Free T4 (16 GB) için ayarlı. Değiştirirken bilinmesi gerekenler:
- `batch=2 × grad_accum=8 = effective 16` — daha düşük OOM riski için `batch=1, grad_accum=16`.
- `max_seq_length=2048` — proje taslağı + sprintler için yeterli; 4K'ya çıkarmak T4'te OOM.
- `lora.r=16` — 4B model için yeterli kapasite; overfit görülürse 8'e indir.
- `lr=2e-4, cosine, warmup 0.05` — Unsloth gemma3 default.
- `fp16=true, bf16=false` — T4 bf16 desteklemiyor.

## Disconnect stratejisi

- `save_steps=100, save_total_limit=3` — Drive'da son 3 checkpoint.
- `trainer.train(resume_from_checkpoint=last_ckpt)` — notebook yeniden başlatılırsa son state'ten devam.
- Colab idle 90dk — notebook'u açık bırak veya Colab Pro düşün (sıfır bütçe tercihi: açık bırak).

## Sonraki faz

- **Faz 3:** `tools/ai-eval/runner.py` — eval seti üzerinde base vs fine-tuned karşılaştırması, metrik raporu.
- **Faz 4:** Ollama Modelfile + docker-compose adapter volume, feature flag ile shadow → %10 → %100.
