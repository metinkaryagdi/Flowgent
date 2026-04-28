# AI Agent Fine-Tune — Faz 0 & Faz 1 Uygulama Raporu

**Tarih:** 2026-04-24
**Branch:** `feature/organization-invite` (fine-tune için ileride `feature/ai-agent-finetune`)
**Plan dökümanı:** [`ai-agent-fine-tuning-plan.md`](./ai-agent-fine-tuning-plan.md)
**Hedef davranışlar:** [`ai-agent-target-behaviors.md`](./ai-agent-target-behaviors.md)

---

## Özet

Plan dökümanında tanımlanan **Faz 0 (Hazırlık)** tamamen bitirildi ve **Faz 1
(Veri Toplama)** için sentetik üretim pipeline'ı uçtan uca kuruldu + smoke
test'le doğrulandı. Hâlâ eksik olan: eval setinin 10 → 20 örnek/feature
genişletilmesi, tam batch üretim (~1050 sentetik örnek), manuel golden set
ve merge script'i. Bunlar planda "Faz 1 tam üretim" altında.

**Üretilen kod:** Python paketi `tools/ai_data_collector/` (10+ modül),
1 Colab notebook iskeleti, 3 eval JSONL dosyası, 2 yeni docs.
**Değiştirilen config:** `.env`, `.env.example`, `.gitignore`, plan doc.
**Doğrulama:** Groq `llama-3.3-70b-versatile` ile 9/9 örnek üretimi geçti.

---

## Faz 0 — Hazırlık (tamamlandı)

Plan dökümanındaki "Hemen Başlanacak" listesindeki 6 atomik adım.

### 1. Auth bug'ları ön-koşulu

**Durum:** Zaten 2026-04-10'da çözülmüştü (memory'deki `bug_authorization_issues.md`
"TAMAMEN ÇÖZÜLDÜ" olarak kayıtlı). Plan dökümanı güncel değildi; `[ ]` olan
madde `[x]` olarak işaretlendi. Kısa özet:
- `Issue` ve `Sprint` entity'lerine `OrganizationId` denormalize edildi (migration'lı).
- `X-Organization-Id` header filtresi read-path'lerde uygulanıyor.
- `OrganizationsController` / `InvitesController` fast-fail: `org_role != "Owner"`
  veya cross-org org_id mismatch → 403.

### 2. Hedef davranış dökümanı

**Oluşturulan dosya:** [`docs/ai-agent-target-behaviors.md`](./ai-agent-target-behaviors.md)

İçerik:
- **Ortak kurallar** (yalnızca JSON, markdown fence yok, Türkçe, PII kopyalama yok)
- **Özellik 1 — `scaffold-project`**: girdi + `ProjectDraft` şeması + örnek (Kulüp Yönetim Sistemi)
- **Özellik 2 — `enrich-issue`**: girdi + `EnrichResult` şeması + örnek (Parola sıfırlama)
- **Özellik 3 — `generate-plan`**: girdi + `PlanResponse` şeması + örnek (Raporlama modülü)
- **Negatif örnekler**: markdown fence, "Elbette işte…" gevezeliği, İngilizce sızıntısı, uydurma alan

Bu döküman fine-tune dataset üretiminin yol haritası ve eval setinin
`must_contain`/`must_not_contain` alanlarının anchor'ı.

### 3. Eval set iskeleti

**Konum:** [`tests/AiEvalDataset/v1/`](../tests/AiEvalDataset/v1/)

Dosyalar:
- `README.md` — format açıklaması (`{id, feature, input, expected_json_shape, must_contain, must_not_contain}`) + evaluation metrikleri
- `scaffold-project.jsonl` — 10 örnek (e-ticaret, eğitim, fintech, sağlık, oyun, devops, mobil, intranet, SaaS B2B, IoT)
- `enrich-issue.jsonl` — 10 örnek (login, parola sıfırlama, sepet ekle, profil foto, fatura PDF, ürün arama, bildirim paneli, Excel export, rol erişim, ödeme)
- `generate-plan.jsonl` — 10 örnek (satış raporu, sınav modülü, düzenli fatura, tele-seans, lider tablosu, metrik, sosyal, izin raporu, abonelik, alarm)

**Parse doğrulaması:** 30/30 satır geçerli JSON, tüm zorunlu alanlar mevcut.

**Kapsam notu:** Plan hedefi 20 örnek/feature. Bir oturumda kaliteli 60 örnek
yazmak yerine 30 sağlam örnek + "Faz 1 başında 20'ye çıkar" notu bıraktım —
daha dürüst bir iskelet.

### 4. Groq API key placeholder

**Değişen dosya:** [`.env.example`](../.env.example)

Eklendi:
```
# ---- AI Fine-tune (tools/ai-finetune + tools/ai-data-collector) ----
GROQ_API_KEY=
HF_TOKEN=
```

Kullanıcı [console.groq.com](https://console.groq.com)'dan ücretsiz key aldı
ve lokal `.env`'e yazdı (2026-04-24). İlk key chat üzerinden sızdı; kullanıcı
revoke + yeniden oluşturdu.

### 5. Colab notebook iskeleti

**Oluşturulan dosya:** [`tools/ai-finetune/colab/train.ipynb`](../tools/ai-finetune/colab/train.ipynb)

Faz 2'de doldurulmak üzere iskelet. Çalışan hücreler:
1. `nvidia-smi` (T4 GPU kontrolü)
2. Unsloth + bitsandbytes + datasets + trl install
3. Drive mount, dataset path assert
4. `FastLanguageModel` import smoke test

Placeholder (TODO) hücreler: dataset load+tokenize, model+LoRA config,
SFTTrainer çağrısı, quick eval, GGUF export.

### 6. `.gitignore` ve memory

**Değişen dosya:** `.gitignore`
```
tools/ai_data_collector/output/
tools/ai_data_collector/golden/*.jsonl
tools/ai-finetune/datasets/
tools/ai-finetune/adapters/
tools/ai-finetune/checkpoints/
*.gguf
```

**Memory kayıt:** `project_ai_finetune_progress.md` ile Faz 0 durumu + sıradaki adımlar.

---

## Faz 1 — Veri Toplama (pipeline kuruldu, tam üretim bekliyor)

### 1. Paket mimarisi

Konum: [`tools/ai_data_collector/`](../tools/ai_data_collector/)

**Not:** Plan dökümanında klasör adı `ai-data-collector` (tire) olarak
yazılmıştı. Python package olarak import edilebilmek için underscore'a
çevrildi (`ai_data_collector`). `.gitignore`, README, örnek komutlar
güncellendi.

```
tools/ai_data_collector/
├── __init__.py
├── README.md                    # kullanım rehberi, klasör yapısı, akış
├── requirements.txt             # python-dotenv, groq>=1.2, pydantic, scikit-learn, tqdm
├── config.py                    # .env loader + sabitler + path'ler
├── domains.py                   # 12 alan × 4-6 alt tür = 50+ varyasyon
├── prompts/
│   ├── __init__.py              # FEATURE_MODULES mapping
│   ├── scaffold_project.py      # 8 template varyantı
│   ├── enrich_issue.py          # 8 template
│   └── generate_plan.py         # 8 template
├── providers/
│   ├── __init__.py
│   ├── base.py                  # Protocol + ProviderError + RateLimit
│   ├── groq_client.py           # llama-3.3-70b-versatile, response_format=json_object
│   └── ollama_client.py         # qwen2.5:7b, format=json, fallback
├── validation/
│   ├── __init__.py
│   ├── schema.py                # feature başına custom hafif validator
│   ├── pii.py                   # email/telefon/TCKN/CC/IBAN/URL regex scrub
│   └── dedup.py                 # TF-IDF char-ngram + cosine @ 0.90
├── synthetic_gen.py             # 2-aşamalı üretim, resume-safe, CLI
├── collect_sessions.py          # ai-db:5439 → PII-scrub → JSONL (psycopg3)
├── golden/                      # manuel hand-crafted örnekler (Faz 1 sonunda doldurulacak)
└── output/                      # üretilen JSONL'ler (git-ignored)
```

### 2. Prompt mimarisi (kalıp tekrarı önleme)

Plan dökümanındaki "kalıp tekrarı önlenmeli" uyarısına cevap:

- **3 özellik × 8 template = 24 farklı prompt kombinasyonu**
- **50+ domain varyasyonu** (e-ticaret, eğitim, fintech, sağlık, oyun,
  devops, mobil, kurumsal içi, SaaS B2B, IoT, lojistik, sosyal) × alt tür ×
  ölçek × kullanıcı tipi
- **Temperature:** her örnekte 0.7-1.0 arası rastgele
- **2-aşamalı üretim:**
  1. Önce kullanıcı talebi LLM'den üretilir (freeform, temperature 0.7-1.0) —
     çeşitlilik kaynağı.
  2. Sonra aynı/fallback LLM bu metinden JSON çıktısı üretir (temperature 0.3)
     — format tutarlılığı.

Bu strateji 1500 örnek üretirken domain dağılımının düz kalmasını engeller.

### 3. Sağlayıcı (provider) katmanı

**Primary: Groq Free Tier**
- Model: `llama-3.3-70b-versatile`
- Free tier RPM ~30
- `response_format={"type": "json_object"}` ile JSON mode
- 429 / `RateLimitError` → `RateLimit` exception → fallback

**Fallback: Ollama**
- Model: `qwen2.5:7b`
- Lokal docker-compose Ollama'ya bağlanır (`OLLAMA_BASE_URL`, default
  `http://host.docker.internal:11434`)
- `format=json` ile JSON mode
- CPU yavaş, sadece Groq quota bittiğinde kullanılır

**`_call_with_fallback` akışı:**
```
try primary.complete()
  except RateLimit: sleep 60sn, fallback.complete()
  except ProviderError: fallback.complete()
```

### 4. Validation üçlüsü

Her üretilen örnek şu üç kapıdan geçer; geçemezse atılır:

**a) Schema validation** (`validation/schema.py`)

Feature başına elle yazılmış hafif validator. Kontroller:
- Root object mu?
- Zorunlu alanlar var mı?
- String uzunluk sınırları (title 5-120, description 10-500, vs.)
- `priority` enum: `{Low, Medium, High, Critical}`
- `storyPoints` Fibonacci: `{1, 2, 3, 5, 8, 13}`
- Sprint count (`scaffold`: 2-4, `plan`: 1-3)
- Issue count per sprint: 3-6
- `project.key` UPPERCASE alphanumeric
- `acceptanceCriteria` en az 3 `- ` maddesi, `edgeCases` en az 2

**b) PII scrub** (`validation/pii.py`)

Regex tabanlı değiştirme (hem input hem output'ta):
- `<EMAIL>`, `<PHONE>`, `<TCKN>`, `<CC>`, `<IBAN>`, `<URL>`
- Recursive dict/list traversal — nested JSON alanlarını kaplar

**c) Dedup** (`validation/dedup.py`)

- TF-IDF char n-gram (3-5) + cosine similarity
- Threshold 0.90
- Incremental: her yeni örnek mevcut korpusa karşı ölçülür
- Resume-safe: JSONL zaten varsa `_load_existing()` ile yüklenir

### 5. `synthetic_gen.py` — üretim script'i

**CLI:**
```bash
python -m tools.ai_data_collector.synthetic_gen \
  --feature scaffold-project|enrich-issue|generate-plan \
  --count 350 \
  --smoke \
  --provider auto|groq|ollama \
  --seed 42
```

**Özellikler:**
- `--smoke` flag'i: sabit 3 örnek (pipeline sağlam mı hızlı test)
- JSONL `a` modunda açılır — mevcut örnekler korunur, yenileri appendlenir
- Her örnek schema + PII + dedup üçlüsünden geçer
- Başarısızlar `[drop:schema]`, `[drop:gen]`, `[drop:dup]` diye loglanır
- Max attempt = `count × 4` — sonsuz döngü guard
- `tqdm` progress bar

**Output JSONL satır formatı:**
```json
{
  "feature": "scaffold-project",
  "template_id": "sp-bullet",
  "domain": {"area": "e-ticaret", "sub": "dijital ürün pazarı", "scale": "orta", "user": "yönetici"},
  "temperature": 0.77,
  "input": {"description": "...", "context": {...}},
  "output": {"project": {...}, "sprints": [...]}
}
```

### 6. `collect_sessions.py` — production sessions çekici

**Kaynak:** `ai-db` container (Docker lokal port 5439).

**SQL filter:**
```sql
SELECT s."Id", s."ProjectId", s."OrganizationId", r."ParsedJson", r."Prompt"
FROM "AiSessions" s
JOIN "AiPlanResults" r ON r."SessionId" = s."Id"
WHERE s."Status" = 2              -- Completed
  AND r."WasApplied" = true       -- kullanıcı onayladı
  AND r."ParsedJson" IS NOT NULL
  AND s."Type" = %s               -- 0=PlanGeneration, 1=IssueEnrichment
ORDER BY s."CreatedAt" DESC
LIMIT %s
```

**Mapping:**
- `AiSessionType.PlanGeneration (0)` → `generate-plan`
- `AiSessionType.IssueEnrichment (1)` → `enrich-issue`
- Diğerleri (Chat, Retrospective, BalanceSuggestion, RiskAssessment) kapsam dışı

**Her kayıt için:**
1. `ParsedJson` parse dene, olmazsa at.
2. Schema validation.
3. `Prompt` son satırından `"Project description:"` / `"Issue title:"` marker'ını yakala → user input'u geri çıkar.
4. PII scrub.
5. JSONL'e yaz.

**Çıktı:** `tools/ai_data_collector/output/sessions-<feature>.jsonl`

Production AI henüz kullanılmadıysa boş dönecek; sorun değil, plan %70
sentetik + %20 sessions + %10 golden dağılımı; sessions yoksa ağırlık
sentetiğe kayar.

### 7. Smoke test sonuçları

Groq `llama-3.3-70b-versatile` ile her özellikten 3 örnek:

| Özellik | Süre | Başarı | Schema drop | Dedup drop |
|---|---|---|---|---|
| scaffold-project | 13 sn | 3/3 | 0 | 0 |
| enrich-issue | 6 sn | 3/3 | 0 | 0 |
| generate-plan | 10 sn | 3/3 | 0 | 0 |

**Doğrulama:**
- 9/9 örnek geçerli JSON
- Tüm `scaffold-project` çıktılarında: 3-4 sprint, 3-4 issue/sprint, UPPERCASE
  key (ETP, KDTP, YHMS), Fibonacci story points
- Domain çeşitliliği: e-ticaret, lojistik, eğitim (3 farklı alan, 3 farklı alt tür)
- Template çeşitliliği: sp-bullet (2×), sp-direct (1×)
- Türkçe çıktılar, İngilizce sızıntısı yok

### 8. Yol üzerinde çözülen sorunlar

**a) Package naming.** Klasör adı `ai-data-collector` (tire) Python
identifier olamıyor. Paket olarak import edilsin diye underscore'a
çevrildi. README, `.gitignore`, komut örnekleri hepsi güncellendi.

**b) groq SDK sürüm uyumsuzluğu.** `groq==0.11` + güncel `httpx 0.28` →
`TypeError: Client.__init__() got an unexpected keyword argument 'proxies'`.
Çözüm: `groq>=1.2,<2` (yeni SDK httpx 0.28 uyumlu). `requirements.txt`
tüm paketleri sürüm pin'leri yerine `>=` formuna alındı (Windows/Linux
uyumluluğu için).

**c) Windows cp1254 console.** `print(f"groq → …")` UnicodeEncodeError.
Çözüm: kod içindeki unicode ok karakterleri (`→`) ASCII `->` yapıldı.
Kalan Türkçe karakterler için komut öncesine `PYTHONIOENCODING=utf-8`
env var ekleniyor.

### 9. Güncellenen / yeni dökümanlar

| Dosya | Tür | Ne |
|---|---|---|
| `docs/ai-agent-target-behaviors.md` | YENİ | 3 özellik için hedef davranış + şema + örnekler |
| `docs/ai-agent-fine-tuning-plan.md` | GÜNCEL | Faz 0 + Faz 1 tooling kısmı tamamlandı olarak işaretlendi |
| `docs/ai-agent-finetune-faz-0-1-rapor.md` | YENİ | bu döküman |
| `tests/AiEvalDataset/v1/README.md` | YENİ | eval dataset formatı |
| `tests/AiEvalDataset/v1/*.jsonl` | YENİ | 30 starter örnek |
| `tools/ai_data_collector/README.md` | YENİ | pipeline kullanım rehberi |
| `tools/ai-finetune/colab/train.ipynb` | YENİ | Colab iskeleti |
| `.env.example` | GÜNCEL | `GROQ_API_KEY` + `HF_TOKEN` |
| `.env` | GÜNCEL | gerçek Groq key (git-ignored) |
| `.gitignore` | GÜNCEL | training data path'leri |

---

## Faz 1 — Kalan işler (gelecek oturum)

Plan dökümanının "Tooling" ve "Hedef boyut" bölümlerinde belirtilen fakat
henüz bitmemiş işler:

1. **Eval setini 20'ye tamamla** (`tests/AiEvalDataset/v1/*.jsonl` her biri 10 → 20).
2. **Tam sentetik batch üretim:**
   ```bash
   PYTHONIOENCODING=utf-8 python -m tools.ai_data_collector.synthetic_gen --feature scaffold-project --count 350
   PYTHONIOENCODING=utf-8 python -m tools.ai_data_collector.synthetic_gen --feature enrich-issue --count 350
   PYTHONIOENCODING=utf-8 python -m tools.ai_data_collector.synthetic_gen --feature generate-plan --count 350
   ```
   Her biri ~30-45 dk Groq üzerinden; gece bırakılabilir.
3. **Sessions toplama (opsiyonel):** Docker compose up sonrası
   `python -m tools.ai_data_collector.collect_sessions --limit 300`.
   Production AI kullanılmadıysa boş dönecek.
4. **Manuel golden set (~100 örnek):** `tools/ai_data_collector/golden/*.jsonl`.
   Eval setiyle çakışmamalı; en kritik/köşe senaryolar elle yazılır.
5. **`merge.py`:** sentetik + sessions + golden → `output/train-v1.jsonl`.
   Eval setindeki input hash'leriyle kesişim silinir, cosine > 0.95
   duplicate'ler elenir. **Henüz yok; Faz 2'ye geçmeden yazılmalı.**

---

## Faz 2 öncesi hazırlık durumu

[`tools/ai-finetune/colab/train.ipynb`](../tools/ai-finetune/colab/train.ipynb)
hazır ama training/eval/export hücreleri `TODO` placeholder. Faz 2'ye
geçmeden önce:

- `tools/ai_data_collector/output/train-v1.jsonl` üretilmiş olmalı
- Dosya Drive'a yüklenmeli: `MyDrive/bp-finetune/datasets/train-v1.jsonl`
- Colab Runtime T4 GPU seçilmeli (Runtime → Change runtime type)
- Hyperparametreler plan dökümanında yazılı: LoRA rank 16, alpha 32,
  dropout 0.05, LR 2e-4 cosine, 3 epoch, batch 2, grad accum 8,
  max_seq_length 2048

---

## Metrik özet

- **Yeni Python dosyası:** 16 (paket modülleri + CLI script'leri + __init__'ler)
- **Yeni doküman:** 3 (`target-behaviors`, `eval README`, bu rapor)
- **Yeni JSONL:** 3 eval starter + 3 smoke test çıktısı (output/, git-ignored)
- **Smoke test:** 9/9 başarılı, toplam ~30 sn, 0 drop
- **Prompt varyant sayısı:** 24 (3 özellik × 8 template)
- **Domain varyant sayısı:** 50+ (12 alan × 4-6 alt tür × 3 ölçek × 5 kullanıcı)

Sıfır bütçe hedefi korundu: tüm üretim Groq free tier + lokal Ollama
fallback; Colab henüz kullanılmadı (Faz 2'ye saklandı).
