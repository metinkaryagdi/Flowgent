# AI Agent Fine-Tuning Planı

**Tarih:** 2026-04-23
**Durum:** Planlama (henüz uygulama yok)
**Branch:** ileride `feature/ai-agent-finetune`
**Bağlı dökümanlar:** [`ai-service-plan.md`](./ai-service-plan.md)

---

## Hedef

Mevcut `gemma3:4b` (Ollama, yerel) modelini, **BitirmeProject domain'ine** (Issue, Sprint, Project, Board, Workflow) özelleşmiş bir **agent** olarak fine-tune etmek. Hedef davranışlar:

1. **Proje iskeleti üretimi** (AI Asistan sayfası): Serbest metinden tutarlı `ProjectDraft` (proje + sprint'ler + issue'lar) JSON çıktısı.
2. **Tool-calling**: Gerektiğinde `create_project`, `create_sprint`, `create_issue`, `assign_issue`, `change_status` gibi sistem fonksiyonlarını **structured output** ile çağırma.
3. **Domain-aware enrichment**: Issue açıklaması/kabul kriteri/edge case üretirken organizasyonun *gerçek* terminolojisini ve geçmiş issue stilini kullanma.
4. **Kısa, deterministik yanıt**: Türkçe, JSON-mode'da format kaymasız, generic LLM "elbette, işte..." gevezeliği olmadan.

**Hedef değil (kapsam dışı):** Genel sohbet, kod yazma, çoklu modalite. Bu işler base model'in default davranışına bırakılacak.

---

## Mevcut Durum Özeti

- **Inference:** Ollama container, `gemma3:4b` (~2.5 GB), CPU-only çalışıyor.
- **Çağrı yüzeyi:** `AiService.Application/Features/*` altında 7 özellik (generate-plan, enrich-issue, detect-duplicate, chat, retrospective, suggest-balance, sprint-risk).
- **Prompt'lar:** Şu an handler'larda string template — dağınık, versiyonsuz.
- **Çıkış formatı:** Bazı handler'lar manuel JSON parse ediyor; format kayması (model freeform metin döndürüyor) en sık başarısızlık sebebi.
- **Eğitim verisi:** Sıfır. Şu ana kadar kullanılan tüm prompt/response çiftleri `AiSessions` tablosunda (DB'de) duruyor ama henüz toplanmadı.

---

## Neden Fine-Tune (LoRA), Neden Full Fine-Tune Değil

| Yaklaşım | Maliyet | Uygunluk | Karar |
|---|---|---|---|
| **Prompt engineering** | Sıfır | Mevcut yöntem; format kayması devam ediyor. | Fine-tune'a paralel iyileştirilecek (taban) |
| **Few-shot in-context** | Sıfır + token | 4B modelin context window'u (8K) için fazla örnek pahalı. | Birlikte kullanılacak ama ana çözüm değil |
| **LoRA / QLoRA** | 1×RTX 3060+ ile birkaç saat | 4B model için optimal; adapter ~50-150 MB | **Tercih edilen yol** |
| **Full fine-tune** | A100 saatleri, 5-10× maliyet | 4B için aşırı, base bilgiyi bozma riski yüksek | Reddedildi |

---

## Faz Planı

### Faz 0 — Hazırlık (3-5 gün, kod yazılmadan)
- [x] **v1 kapsamı kararı:** `scaffold-project`, `enrich-issue`, `generate-plan`. (Karar: 2026-04-23)
- [x] **Veri kaynak dağılımı kararı:** %70 sentetik (Groq + Ollama), %20 mevcut sessions, %10 manuel golden. (Karar: 2026-04-23)
- [x] **Eğitim altyapısı kararı:** Colab Free + lokal RTX 4050. Bütçe $0. (Karar: 2026-04-23)
- [x] **Auth bug'larını kapat:** Tamamlandı 2026-04-10 — memory'deki `bug_authorization_issues.md` kaydı. 3 cross-org sızıntısı kapatıldı (Issue/Sprint için `OrganizationId` denormalizasyonu + `X-Organization-Id` header filtresi), `org_role` controller fast-fail eklendi.
- [x] **Hedef davranış dökümanı:** [`ai-agent-target-behaviors.md`](./ai-agent-target-behaviors.md) yazıldı (2026-04-24). 3 özellik için şema + örnek + negatif örnek.
- [x] **Eval seti iskeleti:** `tests/AiEvalDataset/v1/` altında 3 JSONL dosyası (her biri 10 starter örnek — Faz 1 başında 20'ye çıkarılacak). Format: `{id, feature, input, expected_json_shape, must_contain, must_not_contain}`. README eşlik ediyor.
  - **Bitirme bağlamında:** Bu eval seti aynı zamanda savunma sunumunda "metriklerle iyileştirdim" demenin kanıtıdır. Önemli.

### Faz 1 — Veri Toplama (2 hafta)
**Veri kaynakları (karar sonrası dağılım, sıfır bütçe):**
1. **Sentetik veri (~%70, ~1000 örnek)** — **Groq Free Tier (`llama-3.3-70b-versatile`)** ana üretici, **lokal Ollama (`qwen2.5:7b`)** rate-limit fallback. Çeşitlilik için 8-10 prompt template + 50+ domain × temperature varyasyonu.
2. **Mevcut `AiSessions` tablosu (~%20, ~300 örnek)** — production'dan filtrelenmiş, PII temizlenmiş gerçek örnekler. **Auth bug'ları kapatılmadan bu adıma başlanmaz.**
3. **Manuel golden set (~%10, ~100 örnek)** — en kritik senaryolar elle yazılır; eval seti ile **ÇAKIŞMAMALI** (ayrı tutulur). Bitirme savunmasında "kalite anchor'ı" olarak gösterilebilir.

**Sentetik üretim stratejisi (kritik — kalıp tekrarı önlenmeli):**
- Her özellik için 8-10 farklı prompt template hazırla.
- Her template için 50-100 farklı domain (e-ticaret, fintech, sağlık, eğitim, oyun, IoT, devops, mobil app...) varyasyonu.
- Temperature 0.7-1.0 arası rastgele.
- Üretilen her örnek schema validation'dan geçmeli, geçemeyenler atılmalı.
- Üretim sonrası dedup (cosine similarity > 0.9 olanlar elenir).

**Tooling:**
- [x] `tools/ai_data_collector/` paketi kuruldu (2026-04-24). Python package (tire → underscore). İçerik: `config.py`, `domains.py`, `prompts/{scaffold_project,enrich_issue,generate_plan}.py` (her biri 8 template), `providers/{groq_client,ollama_client}.py`, `validation/{schema,pii,dedup}.py`, `synthetic_gen.py`, `collect_sessions.py`.
- [x] **PII filtresi**: `validation/pii.py` — email, telefon, TCKN, kredi kartı, IBAN, URL regex scrub. Hem `synthetic_gen` hem `collect_sessions` çıktısında aktif.
- [x] Schema validasyonu: `validation/schema.py` — 3 özellik için elle yazılmış hafif validator. Sprint count, issue count, priority enum, Fibonacci story points kontrolleri. Geçemeyen örnek `[drop:schema]` olarak düşer.
- [x] Dedup: `validation/dedup.py` — TF-IDF char-ngram + cosine similarity (threshold 0.90). `should_add` incremental — resume-safe.
- [x] **Smoke test geçti (2026-04-24)**: 3 özellik × 3 örnek = 9 örnek Groq `llama-3.3-70b-versatile` ile ~30sn toplam. 0 drop, tüm şemalar geçti. Çıktılar `tools/ai_data_collector/output/synthetic-*.jsonl` (git-ignored).

**Hedef boyut:** 1000-2000 high-quality örnek. Gemma 4B için fazlası overfitting.

### Faz 2 — Eğitim Pipeline (1 hafta)
- **Framework:** **Unsloth** kesin tercih — Colab Free T4'te resmi olarak destekleniyor, gemma3:4b QLoRA için 8 GB VRAM'da kararlı, Hugging Face `peft+trl` versiyonundan ~2× hızlı.
- **Hyperparameters (Colab Free T4 için optimize):**
  - QLoRA, 4-bit (bnb_4bit_compute_dtype=bfloat16)
  - LoRA rank: 16, alpha: 32, dropout: 0.05
  - Target modules: `q_proj, k_proj, v_proj, o_proj, gate_proj, up_proj, down_proj` (gemma3 standart)
  - LR: 2e-4, cosine schedule, warmup 5%
  - Epochs: 3 (eval ile erken durdur, ~1500 örnekte 2-3 saat)
  - Batch size: 2, gradient accumulation: 8 (effective 16)
  - Gradient checkpointing: **açık** (T4'te VRAM kazandırır)
  - Max seq length: 2048 (proje taslağı için yeter, daha uzunu T4'te OOM)
- **Konum:**
  - `tools/ai-finetune/colab/train.ipynb` — Colab notebook, repo'ya commit
  - `tools/ai-finetune/configs/v1.yaml` — hyperparameter config
  - `tools/ai-finetune/scripts/prepare_dataset.py` — JSONL → HF Dataset format
  - `tools/ai-finetune/scripts/export_to_gguf.py` — adapter → GGUF (Ollama formatı)
- **Colab disconnect stratejisi (kritik):**
  - Her 100 step'te Drive'a checkpoint
  - Eğitim başlamadan Drive mount + dataset upload
  - `google.colab.drive.flush_and_unmount()` ile manuel save
  - Disconnect olursa: notebook yeniden aç, `resume_from_checkpoint=last` ile devam
- **Çıktı:** `adapters/gemma3-4b-bp-v1.gguf` (~120 MB, repo'ya **commit edilmez** — Drive'da veya HF Hub free'de saklanır, link dökümana eklenir).

### Faz 3 — Eval ve Karşılaştırma (3-5 gün)
- [ ] `tools/ai-eval/runner.py` — eval seti üzerinde base ve fine-tuned modeli ardışık koştur, sonuçları yan yana CSV/Markdown rapora dök.
- [ ] **Metrikler (otomatik ölçülen):**
  - Format compliance (JSON parse oranı) — hedef ≥%95
  - Schema compliance (alanlar/tipler doğru mu) — hedef ≥%90
  - Field accuracy (`must_contain` / `must_not_contain` eşleşme oranı) — hedef ≥%80
  - Latency (p50/p95) — base'e göre ≤%20 yavaşlama kabul edilebilir
- [ ] **Manuel kalite skoru:** Sen + 1 ekip arkadaşı (veya danışman) blind A/B (hangi cevap base hangi fine-tuned bilmeden) 1-5 arası puanla. Hedef: fine-tuned ortalaması base + 0.7.
- [ ] **Bitirme savunması için kritik:** Bu raporu (`docs/ai-finetune-eval-v1.md`) commit et — savunmada "fine-tune'un ölçülebilir faydası şu" diye gösterilecek.
- [ ] Karar matrisi: tüm otomatik metrikler base'i geçtiyse → faz 4'e geç. Geçmediyse → veri kalitesini iyileştir, hyperparameter tarat (Colab session quota'sı dikkate al — 3-4 deneme ile sınırlı).

### Faz 4 — Deploy (3-5 gün)
- [ ] Ollama'ya custom model push: `Modelfile` ile base + adapter birleştir.
  ```
  FROM gemma3:4b
  ADAPTER ./adapters/gemma3-4b-bp-v1.gguf
  PARAMETER temperature 0.3
  PARAMETER top_p 0.9
  SYSTEM "Sen BitirmeProject AI agent'ısın..."
  ```
- [ ] `docker-compose.yml`: `ollama` service'ine adapter volume mount + initContainer'da `ollama create bp-agent -f Modelfile`.
- [ ] `appsettings.json` → `Ollama:Model: "bp-agent"` (default `gemma3:4b`'den geçiş).
- [ ] **Feature flag:** `Ollama:UseFinetuned` = false default. Önce shadow mode (her iki modele de çağrı, response'u logla, kullanıcıya base'i göster), sonra %10 → %50 → %100 trafik.
- [ ] **Rollback planı:** Flag false → tek deploy, anında base modele döner.

### Faz 5 — Tool-Calling / Agent Davranışı (Faz 1-3 ile **paralel** ilerler — karar verildi)
**Neden paralel:** Tool-calling fine-tune için **eğitim verisi** ister (tool çağrılarının doğru formatta örnekleri). İskelet hazır olmadan bu örnekler üretilemez. Aynı zamanda fine-tune sürerken (haftalık iş yükü düşük, çoğu zaman Colab beklemesi) bu disiplinde gerçek kod yazılır.
- [ ] `AiService.Application/Tools/` altında `ITool` interface tanımla:
  ```csharp
  public interface ITool
  {
      string Name { get; }
      string Description { get; }
      JsonSchema InputSchema { get; }
      Task<object> ExecuteAsync(JsonElement input, CancellationToken ct);
  }
  ```
- [ ] İlk parti tool'lar: `CreateProjectTool`, `CreateSprintTool`, `CreateIssueTool`, `AssignIssueTool`, `ChangeStatusTool`. Hepsi mevcut `IIssueServiceClient` / `ISprintServiceClient` üzerinden gider — yeni endpoint açılmaz, **mevcut yetkilendirme korunur**.
- [ ] Agent loop: model → tool çağrısı parse et → execute et → sonucu modele geri ver → final response. Max 5 iter (sonsuz döngü guard).
- [ ] **Audit:** Her tool execution `AiToolExecutions` tablosuna yazılır (kim/ne zaman/hangi tool/input/output/duration/success). Kullanıcı sonradan "AI bunu sen mi yaptın?" diyebilsin.

### Faz 6 — Sürdürülebilirlik (bitirme sonrası opsiyonel)
**Bitirme savunması için zorunlu:**
- [ ] Eval raporunu commit et: `docs/ai-finetune-eval-v1.md` (Faz 3 çıktısı).
- [ ] README/SUNUM güncellemesi: "AI Agent" bölümü, fine-tune metodolojisi, before/after metrik tablosu.

**Bitirme sonrası (opsiyonel, projeyi sürdürürsen):**
- [ ] **Feedback loop:** Frontend'e "👍/👎 + sebep" butonu ekle. 👎 alan response'lar `AiFailedResponses` tablosuna düşer → v2 datasetine girer.
- [ ] Her release'de eval seti çalıştır (CI'da değil — manuel; çünkü Ollama CI'da yok). Regression varsa block.
- [ ] 6 ayda bir veri tazele, adapter v2 eğit. Her versiyon eval karşılaştırmasıyla kabul edilir, "vibe check" ile değil.

---

## Risk Listesi (Sıfır Bütçe Bağlamında Güncellenmiş)

| Risk | Olasılık | Etki | Azaltma |
|---|---|---|---|
| Eğitim datası az → underfit | Orta | Orta | Sentetik veri %70, 100 manuel golden, 3 epoch + erken durdur |
| Aşırı eğitim → general capability kaybı | Yüksek | Yüksek | LoRA rank 16, eval setinde "out-of-domain" örnekleri tut |
| PII sızıntısı (sessions'tan training'e) | Orta | Çok Yüksek | Otomatik masking + manuel review + auth bug ön-koşul + dataset .gitignore |
| Üretim modeli hatalı tool çağırıyor → veri bozuyor | Düşük | Çok Yüksek | Tool exec'te org_id whitelist, tüm mutasyonlarda audit, ilk hafta dry-run mode |
| Ollama LoRA adapter formatı değişir | Düşük | Orta | GGUF formatında export, base model versiyonunu pin'le (`gemma3:4b` tag) |
| **Colab Free idle disconnect orta eğitimde** | Yüksek | Orta | Her 100 step'te Drive checkpoint, `resume_from_checkpoint` |
| **Groq Free Tier rate limit / kapanma** | Orta | Orta | Lokal Ollama (`qwen2.5:7b`) fallback, gece batch üretim |
| **Colab GPU quota tükenir (haftalık)** | Orta | Yüksek | Hyperparameter tarama 3-4 deneme ile sınırlı, smoke test'i lokalde yap |
| **Bitirme zaman baskısı** | Yüksek | Yüksek | Yedek plan: gemma3:1b'ye düş, lokal tam eğitim, savunmada trade-off açıkla |
| **Auth bug'ları kapanmaz** | Orta | Çok Yüksek | Sessions'ı **hiç kullanma**, tamamen sentetik git (kalite düşer ama temiz) |

---

## Karar Noktaları — KARARLAR (2026-04-23)

1. **v1 kapsamı (3 özellik):** ✅ `scaffold-project`, `enrich-issue`, `generate-plan`.
2. **Veri kaynağı:** ✅ Sentetik veri ana kaynak (~%70). Mevcut `AiSessions`'tan filtrelenmiş örnekler %20, manuel golden set %10 (~100 örnek).
3. **Eğitim altyapısı:** ✅ **Tamamen ücretsiz** — lokal RTX 4050 (6 GB) prototip/eval için, **Google Colab Free (T4 16 GB)** gerçek eğitim için. Bütçe: $0 (bitirme projesi).
4. **Tool-calling sırası:** ✅ Paralel — Faz 1-3 yürürken Faz 5 iskeleti (ITool interface, ilk 5 tool) kurulur.
5. **Auth bug ön-koşulu:** ✅ Memory'deki `bug_authorization_issues.md` Faz 0'dan ÖNCE kapatılır. Aksi halde mevcut sessions'ta cross-org sızıntı varsa training data zehirlenir.

---

## Donanım Kararı Detayı (RTX 4050 6 GB + Colab Free)

| Konfigürasyon | VRAM ihtiyacı | Nerede çalışır? | Kalite | Karar |
|---|---|---|---|---|
| gemma3:4b QLoRA, default | ~8 GB | ❌ 4050'de OOM | Yüksek | Lokal yok, Colab var |
| gemma3:4b QLoRA + grad checkpoint + batch 1 + seq 1024 | ~5.5 GB | ⚠ 4050'de sınırda | Yüksek | Sadece smoke test |
| gemma3:4b QLoRA, **Colab Free T4 (16 GB)** | ~8 GB rahat | ✅ | Yüksek | **Üretim eğitimi** |
| gemma3:1b QLoRA, lokal 4050 | ~3.5 GB | ✅ Rahat | Orta-düşük | Yedek plan |

**Çalışma yöntemi (sıfır bütçe):**
- **Lokal (4050 6 GB):**
  - Pipeline kurulumu, kod debug, dataset hazırlama scriptleri
  - 50-100 örneklik mini dataset ile **smoke test** (training loop sağlam mı?)
  - Eğitilmiş adapter'ın inference testi (Ollama)
  - Eval seti çalıştırma (sadece inference, eğitim değil)
- **Google Colab Free (T4 16 GB):**
  - Tam dataset (1500 örnek) × 3 epoch ≈ 2-3 saat → tek session içinde rahat tamamlanır
  - Idle disconnect riski: training script'inde her epoch sonu Drive'a checkpoint yaz
  - Notebook konumu: `tools/ai-finetune/colab/train.ipynb` (repo'ya commitlenir, Colab'tan açılır)
  - Drive entegrasyonu: dataset Drive'da, çıktı adapter Drive'a yazılır, sonra lokale çekilir

**Yedek plan (Colab da yetmezse):**
- gemma3:1b'ye düş — 4050'de tam lokal eğitim yapılır, kalite metriklerinden taviz verilir
- Dökümanda "donanım kısıtı nedeniyle model küçültüldü" notu düşülür
- Bitirme savunması için bu trade-off gerekçeli olarak sunulur

**Sentetik veri üretimi (sıfır bütçe):**
- **Ücretli plan reddedildi:** GPT-4 API ($25) ❌
- **Ücretsiz alternatif:** Üç katmanlı yaklaşım:
  1. **Groq Free Tier** — `llama-3.3-70b-versatile` modeli, dakikada ~30 istek limiti, ama gece üretimle 1000 örnek 2-3 saatte biter. API key ücretsiz ([groq.com](https://groq.com)).
  2. **Lokal Ollama** — büyük modeller (`qwen2.5:7b`, `llama3.1:8b`) lokalde indirilir, CPU'da yavaş ama ücretsiz; gece bırakılır.
  3. **Hugging Face Inference API Free** — günlük limit var ama yeter, fallback olarak.
- Üretim script'i: `tools/ai-data-collector/synthetic_gen.py` — Groq'u dener, rate limit'e takılırsa Ollama'ya düşer.

---

## Bütçe Güncellemesi (Sıfır Bütçe — Bitirme Projesi)

| Kalem | Maliyet | Kaynak |
|---|---|---|
| Eğitim GPU | **$0** | Google Colab Free (T4 16 GB) |
| Sentetik veri | **$0** | Groq Free Tier + lokal Ollama (qwen2.5:7b) |
| Mevcut sessions filtreleme + manuel golden | **$0** | İç emek |
| Inference (üretim) | **$0** | Mevcut Ollama, CPU |
| Lokal smoke test / eval | **$0** | Mevcut RTX 4050 |
| **Toplam** | **$0** | — |

**Trade-off'lar (ücretsizliğin maliyeti):**
- Colab idle disconnect → checkpoint stratejisi şart
- Groq dakikada 30 istek → 1000 örnek üretimi 1 günü bulabilir (planla, otomatik bırak)
- Lokal Ollama büyük model CPU → çok yavaş, sadece gece kullanılabilir
- Hyperparameter taraması sınırlı (her deneme Colab session'ı tüketir) → 3-4 deneme ile sınırlı kal

---

## Bağımlılıklar

- Memory'deki `bug_authorization_issues.md` cross-org sızıntıları **fine-tune datası toplamadan önce** kapatılmalı. Yoksa training set'e başka org'un verisi sızar. Kapatılamazsa Risk listesindeki yedek plan: tamamen sentetik git.
- [`next-phases-plan.md`](./next-phases-plan.md)'de F1-F4 tamamlandı, AI servis fazı (AI-1) bekliyor. Bu döküman AI-1'in **alt fazı** (AI-1.5) olarak ele alınabilir.
- `tools/` klasörü repo'da yok — Faz 1'de oluşturulacak. **Tüm scriptler aynı repo'da** (sıfır bütçe → ayrı private repo bedava değil), dataset `.gitignore`'da, çıktı adapter Drive/HF Hub'da.

---

## Zaman Çizelgesi (Sıfır Bütçe + Bitirme Takvimi)

Toplam takvim süresi: **~6 hafta** (yarı zamanlı, başka bitirme işleri paralel sürerken). Tam zamanlı çalışılırsa 3 haftaya iner ama gerçekçi değil.

| Hafta | Faz | Kritik çıktı | Sıfır bütçe etkisi |
|---|---|---|---|
| **1** | Faz 0 (Hazırlık) + Auth bug fix | Eval seti (60 örnek) commit, auth bug'ları kapalı | — |
| **2-3** | Faz 1 (Veri) + Faz 5 başlangıç (Tool iskeleti) | 1500 sentetik + 100 manuel + 300 sessions JSONL | Groq quota → gece batch, ~2 gece |
| **4** | Faz 2 (Eğitim) + Faz 5 devam | `gemma3-4b-bp-v1.gguf` adapter | Colab session quota → 2-3 deneme tavanı |
| **5** | Faz 3 (Eval) + Faz 4 (Deploy) | Eval raporu, Ollama Modelfile, feature flag | — |
| **6** | Faz 5 tamamla + Faz 6 (Savunma hazırlığı) | Tool-calling agent loop, README/SUNUM güncelle | — |

**Not:** Colab Free GPU quota'sı haftalık ~12 saat T4 verir. 3 epoch × 1500 örnek = ~3 saat. Yani teorik olarak haftada 4 deneme; pratikte 2-3.

---

## Başarı Kriterleri (Bitirme Savunması İçin)

Aşağıdakilerin tamamı sağlanırsa savunmada **"AI agent fine-tuning yaptım"** denilebilir; eksiklerse "AI entegrasyonu yaptım, fine-tuning gelecek iş" denir.

**Minimum kabul (savunmaya yeterli):**
- [ ] En az 1 özellik (`scaffold-project`) için fine-tuned model **production'da çalışıyor** (Ollama'da, feature flag açık).
- [ ] Eval raporu (before/after metrik tablosu) commit edilmiş.
- [ ] Metriklerden en az 3'ü base modeli geçmiş (Format / Schema / Field accuracy).
- [ ] AI Asistan UI'sı fine-tuned modele bağlı çalışıyor.
- [ ] Demo edilebilir: serbest metin → proje taslağı → onay → gerçek proje/sprint/issue oluşumu.

**İyi olur (bonus):**
- [ ] 3 özelliğin hepsi fine-tuned.
- [ ] Tool-calling agent loop çalışıyor (model gerçek `create_issue` çağırıyor).
- [ ] Eval setine kullanıcı feedback'inden 20+ yeni örnek eklenmiş.
- [ ] Adapter HF Hub'da public, paylaşılabilir.

**Yapma (kapsam dışı, savunmada bahsetme):**
- General sohbet kalitesi
- Türkçe dışı diller
- Multimodal (görsel/ses)
- RLHF / preference learning

---

## Bu Plan Ne Zaman Güncellenir

- Faz 0 başlamadan önce: kapsam ve eval seti onaylanınca (✅ kapsam onaylandı 2026-04-23).
- Her faz sonunda: gerçekleşen vs planlanan + öğrenilenler.
- Savunma sonrası: jüri geri bildirimi ışığında v2 öncesi.

---

## Hemen Başlanacak (Faz 0 Bu Hafta)

Sıralı, atomik adımlar — her biri 1-2 saat:

1. [x] **Auth bug'larını kapat** — 2026-04-10 tarihinde tamamlandı. Memory'deki `bug_authorization_issues.md` ("TAMAMEN ÇÖZÜLDÜ"). Denormalizasyon + controller fast-fail + handler DB kontrolü üçlüsü yerli yerinde.
2. [x] **Hedef davranış dökümanı:** [`ai-agent-target-behaviors.md`](./ai-agent-target-behaviors.md) — 3 özellik, her biri için şema + örnek girdi-çıktı + ortak kurallar + negatif örnekler. (2026-04-24)
3. [x] **Eval seti iskeleti:** `tests/AiEvalDataset/v1/` altında 3 JSONL + README. Her dosya 10 starter örnek (domain-çeşitli: e-ticaret, eğitim, fintech, sağlık, oyun, devops, mobil, kurumsal içi, SaaS B2B, IoT). Faz 1 başında 20'ye çıkarılacak. (2026-04-24)
4. [x] **Groq API key placeholder:** `.env.example`'da `GROQ_API_KEY=` ve `HF_TOKEN=` satırları eklendi. **Kullanıcı aksiyonu:** [console.groq.com](https://console.groq.com) → ücretsiz key al → lokal `.env`'e yaz. (2026-04-24)
5. [x] **Colab notebook iskeleti:** `tools/ai-finetune/colab/train.ipynb` — GPU kontrolü + Unsloth install + Drive mount + import smoke test çalışır; training/eval/export hücreleri Faz 2 için `TODO` placeholder. (2026-04-24)
6. [x] **Faz 0 tamamlandı.** (2026-04-24) Faz 1'e geçiş için ön-koşul: Groq API key kullanıcıdan alınır, sonra `tools/ai-data-collector/` script'leri yazılır.

**Kalan kullanıcı aksiyonu:** Groq API key'i almak (3 dk'lık iş), yoksa Faz 1 sentetik üretim lokal Ollama'ya düşer — plan dökümanındaki "trade-off" bölümü geçerli olur.
