# AI Agent Fine-tune — 2026-04-25 İlerleme Raporu

**Branch:** `feature/organization-invite` (fine-tune kapsamı için ileride `feature/ai-agent-finetune`)
**Önceki rapor:** [`ai-agent-finetune-faz-0-1-rapor.md`](./ai-agent-finetune-faz-0-1-rapor.md) — 2026-04-24
**Bağlı:** [`ai-agent-fine-tuning-plan.md`](./ai-agent-fine-tuning-plan.md), [`ai-agent-target-behaviors.md`](./ai-agent-target-behaviors.md), [`ai-finetune-eval-v1.md`](./ai-finetune-eval-v1.md)

---

## Özet

24 saatte yapılan iş:
- **Faz 1** kısmi devam — Groq free-tier günlük TPD limit'i ~50 örnek/key'de tükeniyor; 4 hesap döngüsü ile 318+ sentetik örnek üretildi (devam ediyor).
- **Faz 2** kodu komple yazıldı (config + 3 script + Colab notebook).
- **Faz 3** eval runner kuruldu, base `gemma3:4b` ile smoke test çalıştırıldı (schema **%0**).
- **Faz 4** deploy altyapısı (Modelfile + feature flag) eklendi.
- **Faz 5** tool-calling sistemi kodlandı (interface + 5 tool + agent loop + audit entity + migration + endpoint).
- **Faz 6** savunma raporu şablonu oluşturuldu.

`.NET` build: **0 error, 0 warning** (AiService.Api).

---

## Faz 1 — Veri Toplama (Groq quota macerası)

### Durum (rapor anı, batch hâlâ çalışıyor)

| Kaynak | Hedef | Mevcut | Açıklama |
|---|---:|---:|---|
| Sentetik scaffold-project | 350 | 169 | 1. ve 4. key katkısı (sıra: 60→116→169) |
| Sentetik enrich-issue | 350 | **143+** | 4. key aktif, +5.7sn/it tempoda devam ediyor |
| Sentetik generate-plan | 350 | 3 | sıra bekliyor (chain'in 3. adımı) |
| Manuel golden | ~100 | 45 | 15-15-15, schema 45/45 ✅ |
| Eval (test) | 60 | 60 | 20-20-20, training'e dahil edilmiyor |

### Groq quota gözlemi

| Key | Prefix | Üretilen | Tukeniş noktası |
|---|---|---:|---|
| 1 (orijinal) | (eski) | 57 | rate-limit retry loop'u |
| 2 (yeni hesap) | `gsk_iK...` | 56 | aynı pattern |
| 3 (yeni hesap) | `gsk_nQ...` | 53 | aynı pattern |
| 4 (yeni hesap) | `gsk_nQ...` | **140+ (hâlâ aktif)** | quota dolu hesap, gerçek dataset bu key'le ilerliyor |

**Pattern:** Groq Free Tier için `llama-3.3-70b-versatile` modelinde günlük TPD ~250-300K token (aktarım: ~50-55 örnek). 4. hesap muhtemelen tam fresh quota ile geldi veya Groq quota tracking'de tutarsızlık oldu.

### Sıralama kararı

Önceki turda scaffold zaten 169'da iken yeni key'lerle scaffold'u zorlamak dataset'i dengesiz bıraktı (enrich/plan 3'te). 4. key ile **chain sırası değiştirildi:**

```
enrich +346 → plan +347 → scaffold +181 → DONE.flag → merge.py
```

Bu denge stratejisi: tek key bile tükense her 3 feature'da minimum ~50 örnek olur.

### Dropouts

Şu ana kadar 318 üretimde **0 schema drop, 0 PII drop, 0 dedup drop** raporlandı. Pipeline sağlam.

---

## Faz 2 — Eğitim Pipeline (kod komple)

Yeni dosyalar:

| Dosya | İş |
|---|---|
| [`tools/ai-finetune/configs/v1.yaml`](../tools/ai-finetune/configs/v1.yaml) | QLoRA hyperparametreler: r=16 α=32 dropout=0.05, lr=2e-4 cosine, 3 epoch, fp16, T4 optimize |
| [`tools/ai-finetune/scripts/prepare_dataset.py`](../tools/ai-finetune/scripts/prepare_dataset.py) | merge çıktısını validate + 90/10 split + token p50/p95 raporu |
| [`tools/ai-finetune/scripts/export_to_gguf.py`](../tools/ai-finetune/scripts/export_to_gguf.py) | Lokal post-hoc LoRA → GGUF (llama.cpp convert_lora_to_gguf) + Modelfile taslak |
| [`tools/ai-finetune/colab/train.ipynb`](../tools/ai-finetune/colab/train.ipynb) | 9 code cell komple pipeline: GPU → install → Drive → model+LoRA → dataset → SFTTrainer → quick eval → export |
| [`tools/ai-finetune/README.md`](../tools/ai-finetune/README.md) | Faz 1→2→3→4 akış rehberi, Drive düzeni, disconnect stratejisi |

Eğitim Faz 1 tamamlanınca Drive'a upload + Colab T4 ile ~2-3 saat sürer.

---

## Faz 3 — Eval Runner (kod + base smoke)

[`tools/ai_eval/`](../tools/ai_eval/) Python paketi:
- `metrics.py` — format compliance, schema compliance, field accuracy (must_contain/not_contain), latency p50/p95
- `providers.py` — Ollama HTTP `/api/chat`, `format=json`, `temperature=0.2`
- `runner.py` — `--models a,b --features ... --limit N` → raw JSONL + summary.json + report.md

### Base smoke (2026-04-25 01:27)

`gemma3:4b` × enrich-issue × 2 örnek:
- Format compliance: **100%** (parse OK)
- Schema compliance: **0%** (`acceptanceCriteria` string yerine list geliyor)
- Field accuracy: 50%
- Latency p50: 14.2s

**Bitirme savunması için:** Bu sonuç fine-tune'un gerçek değer katacağının kanıtı. Base zaten JSON üretiyor ama bizim şemamızı tutturamıyor — fine-tune sonrası schema %90+ hedef gerçekçi.

---

## Faz 4 — Deploy Altyapısı

| Dosya | İş |
|---|---|
| [`docker/ollama/Modelfile`](../docker/ollama/Modelfile) | Base + ADAPTER placeholder (yorumlu, adapter beklerken pasif) |
| [`docker/ollama/README.md`](../docker/ollama/README.md) | Adapter dağıtım rehberi + rollback + shadow mode notları |
| `appsettings.json` | `Ollama:UseFinetuned: false`, `Ollama:FinetunedModel: bp-agent` |
| `docker-compose.yml` | `Ollama__UseFinetuned`, `Ollama__FinetunedModel` env'leri |
| `OllamaClient.cs` | Flag'e göre model seçer, log atar (`Ollama using fine-tuned model {Model} (base: {Base})`) |

**Default `false`** → runtime davranışı şu an değişmedi. Adapter `.gguf` üretildiğinde:
1. `docker/ollama/`a kopyala
2. `Modelfile`'da `ADAPTER` satırının yorumunu aç
3. `Ollama:UseFinetuned: true` yap
4. `ollama create bp-agent -f Modelfile`

---

## Faz 5 — Tool-Calling

### Application katmanı

[`src/services/ai/AiService.Application/Tools/`](../src/services/ai/AiService.Application/Tools/):

```
ITool.cs            — Name, Description, InputSchema (JsonElement), ExecuteAsync
ToolContext.cs      — record(UserId, OrganizationId, ProjectId, SessionId?)
ToolResult.cs       — record(Success, Data, Error) + Ok/Fail factory
ToolSchemas.cs      — JSON Schema parse helper
ToolRegistry.cs     — IToolRegistry: All, Get(name), GetCatalogJson()
AgentLoop.cs        — max 5 iter, JSON tool_calls/final dispatch, audit her tool execution
└── Impl/
    ├── CreateIssueTool.cs           — title + priority + opt description
    ├── CreateSprintTool.cs          — name + goal
    ├── AddIssueToSprintTool.cs      — sprintId + issueId
    ├── GetActiveSprintTool.cs       — () → ActiveSprintDto?
    └── GetProjectIssuesTool.cs      — () → List<ProjectIssueDto>
```

Tool'lar mevcut `IIssueServiceClient`/`ISprintServiceClient` üzerinden gider — yeni endpoint açılmaz, mevcut yetkilendirme korunur.

### Audit (Domain + Infrastructure)

| Katman | Dosya |
|---|---|
| Domain | [`AiToolExecution.cs`](../src/services/ai/AiService.Domain/Entities/AiToolExecution.cs) — Id, SessionId?, UserId, OrgId, ProjectId, ToolName, InputJson, OutputJson?, Success, ErrorMessage?, DurationMs |
| Application | [`IAiToolExecutionRepository.cs`](../src/services/ai/AiService.Application/Abstractions/IAiToolExecutionRepository.cs) |
| Infrastructure | [`AiToolExecutionConfiguration.cs`](../src/services/ai/AiService.Infrastructure/Persistence/Configurations/AiToolExecutionConfiguration.cs), [`AiToolExecutionRepository.cs`](../src/services/ai/AiService.Infrastructure/Repositories/AiToolExecutionRepository.cs) |
| Migration | `20260425074806_AddAiToolExecutions` — 2 index (Org+Project+CreatedAt, SessionId) |

`AgentLoop` her tool execution'ı `Stopwatch` ile ölçer ve atomik olarak `AiToolExecutions` tablosuna yazar.

### Endpoint

```
POST /api/v1/ai/agent
Body: { projectId, message, sessionId? }
Response: { finalText, iterationsUsed, hitIterationLimit, turns: [{kind, content}] }
```

System prompt agent için: tool catalog + sadece JSON döndürme zorunluluğu + tool tekrar etme yasağı + Türkçe.

### DI

`AddAiApplication()` 5 tool + ToolRegistry + AgentLoop'u `Scoped` olarak kaydeder (HttpClient bağımlısı tool'lar için scoped şart).

### Build

`dotnet build src/services/ai/AiService.Api`: **0 error, 0 warning**.

---

## Faz 6 — Savunma Raporu Şablonu

[`docs/ai-finetune-eval-v1.md`](./ai-finetune-eval-v1.md) hazır. İçerik:
- Karşılaştırma tablosu (base vs fine-tuned, 3 feature × metrik)
- Hedefler (Format ≥%95, Schema ≥%90, Field ≥%80, Latency p95 ≤+%20)
- Manuel kalite skoru (blind A/B) prosedürü
- Kabul kararı checklist
- 3 örnek karşılaştırma format'ı (raw-*.jsonl'dan)
- Komutlar (`runner.py --models gemma3:4b,bp-agent`)

Eğitim sonrası tablolar `runner.py` çıktısıyla doldurulur.

---

## Bugünün İstatistikleri

| Metrik | Değer |
|---|---|
| Yeni dosya | 21 (.cs + .py + .ipynb + .yaml + .md + Migration) |
| Değişen dosya | 5 (DI, DbContext, OllamaClient, AiController, appsettings, docker-compose) |
| Yeni satır kod (.cs + .py + .yaml) | ~1500 |
| .NET build | 0 error, 0 warning |
| Sentetik örnek (Faz 1) | 175 → 318+ (devam ediyor) |
| Groq hesap döngüsü | 4 |
| Eval base smoke | 100% format / 0% schema (referans noktası) |

---

## Bugünden Sonra (yarın için)

### Faz 1 tamamlama (öncelik 1)

4. key bittiğinde dağılım belli olur. Senaryo:
- **A.** 4. key dataset'i tamamlar (350×3) → direkt prepare_dataset → Drive → Colab
- **B.** Yine ~50'de tükenir → 5. key + chain devam (kalan kısım)
- **C.** Augmentation script (output paraphrase × 3, lokal Ollama gemma3:4b) ile mevcut sayıyı 2-3× büyüt → pilot eğitim

Mevcut tempo (4. key) sürerse senaryo A güçlü.

### Sonraki komut hafızası

```bash
# Faz 1 resume (gerekirse, mevcut sayıdan kalanı hesapla)
PYTHONIOENCODING=utf-8 python -m tools.ai_data_collector.synthetic_gen --feature <f> --count <kalan> --provider groq

# Merge (otomatik watcher zaten var; manuel için)
python -m tools.ai_data_collector.merge --out tools/ai_data_collector/output/train-v1.jsonl

# Faz 2 — lokal prepare
python tools/ai-finetune/scripts/prepare_dataset.py

# Faz 3 — eval (base only, ft yok henüz)
python -m tools.ai_eval.runner --models gemma3:4b --limit 3
```

### Bitirme zaman çizelgesinde durum

Plan dökümanında Hafta 2-3 = Faz 1 + Faz 5. Bugün:
- Faz 1: %35-50 (kalan: scaffold +181 ya da chain bittiğinde tüm hedef)
- Faz 5: ✅ tamam (önceden plan'da "paralel" çalışılması öneriliyordu, bugün yapıldı)
- Faz 2/3/4/6 altyapıları: ✅ tamam (gelecek haftaları da "veri ve eğitim" odağına bıraktı)

Faz 1 tamamlanır tamamlanmaz Colab'a geçilebilir → Hafta 4'ün başlangıcı plana göre.

---

## Sonraki Oturum Açılış Kontrol Listesi

1. `wc -l tools/ai_data_collector/output/synthetic-*.jsonl` → Faz 1 son durum
2. `ls tools/ai_data_collector/logs/` → DONE.flag, MERGE_DONE.flag, train-v1.jsonl var mı?
3. Eğer batch hâlâ çalışıyorsa: `tail -c 1000 logs/<active>.log | tr '\r' '\n' | tail -5`
4. Eğer batch durdu ve dataset eksikse: 5. key veya augmentation kararı
5. Eğer dataset tam: `prepare_dataset.py` → Drive upload → Colab `train.ipynb`

---

## Memory Güncel

`project_ai_finetune_progress.md` 2026-04-25 itibariyle güncel. MEMORY.md'de pointer:
> AI Fine-tune Progress — Faz 0/2/3/4/5/6 ✅ kod tamam; Faz 1 batch Groq limit nedeniyle parça parça ilerliyor (4 hesap döngüsü).
