# AI Servis — Uygulama Planı

**Branch:** `feature/ai-service` (ilerleyen aşamada açılacak)  
**Tarih:** 2026-04-01

---

## Genel Mimari

```
Browser → Gateway → AiService (port 5008)
                        ↓
                 Ollama (port 11434)  — yerel LLM
                        ↓
         SprintService (iç ağ, direkt HTTP)
         IssueService  (iç ağ, direkt HTTP)
```

**Yeni servis:** `AiService` (port 5008)  
**LLM:** Ollama Docker container (port 11434)  
**Model:** `gemma3:4b` — RTX 4050 6GB VRAM (~3.2GB kullanır, Türkçe iyi)  
**Fallback:** `llama3.2:3b` — JSON parse başarısız olursa  
**Yeni DB:** `ai-db` PostgreSQL (port 5439) — session geçmişi

---

## İç Servis İletişim Kararı (Hibrit)

| Yön | Yol | Neden |
|-----|-----|-------|
| Browser → AiService | Gateway üzerinden (JWT doğrulanır) | Güvenlik, tutarlılık |
| AiService → SprintService | Docker iç ağ, direkt HTTP | Hız, basitlik |
| AiService → IssueService | Docker iç ağ, direkt HTTP | Hız, basitlik |

**Internal call kimlik doğrulama:**
```
X-Internal-Service: AiService
X-User-Id: <userId>
```
SprintService ve IssueService bu header'ı görünce JWT doğrulamayı bypass eder.

---

## Özellikler

### 1. Plan Oluşturma (Temel Özellik)
Kullanıcı proje açıklaması yazar → AI sprint + issue planı oluşturur → servislere kaydeder.

```
POST /ai/generate-plan
{ "projectId": "...", "description": "E-ticaret sitesi, 3 sprint..." }
```

**Çıktı:** Oluşturulan sprint ve issue özeti, session kaydı

---

### 2. Issue Açıklama Zenginleştirme
"Login sayfası yap" → AI: Description + Acceptance Criteria + Edge Cases + Story Point önerisi

```
POST /ai/enrich-issue
{ "issueId": "...", "title": "Login sayfası yap" }
```

Her issue detay sayfasında "AI ile Zenginleştir" butonu.

---

### 3. Proje Sorgulama — RAG-lite Chat
Chatbot arayüzünde doğal dil ile proje soruları.

```
POST /ai/chat
{ "projectId": "...", "sessionId": "...", "message": "Bu sprint'te hangi issue'lar açık?" }
```

**Yöntem:** DB'den ilgili proje/sprint/issue verisi çekilip prompt'a context olarak eklenir (context injection, tam RAG değil). `gemma3:4b` bu görev için yeterli.

---

### 4. Sprint Retrospektif Özeti (Event-Driven, Otomatik)
`SprintCompletedEvent` consume edilir → AI analiz üretir → NotificationService ile bildirim gönderilir.

**Analiz içeriği:** Tamamlanan/geciken issue sayısı, gecikme sebepleri, bir sonraki sprint önerileri.

---

### 5. Sprint Yük Dengeleme Önerisi
Kişi başı story point hesaplayarak aşırı yüklenmiş üyeleri tespit eder.

```
POST /ai/suggest-balance
{ "sprintId": "..." }
```

---

### 6. Benzer Issue Tespiti
Yeni issue oluşturulurken duplicate kontrolü yapar.

```
POST /ai/detect-duplicate
{ "projectId": "...", "title": "Şifremi unuttum akışı" }
```

---

### 7. Gecikme Risk Tahmini
Sprint ortasında ilerleme oranına bakarak risk skoru üretir.

```
GET /ai/sprint-risk?sprintId=...
```

---

## Servis Mimarisi (Clean Architecture)

```
src/services/ai/
├── AiService.Api/
│   ├── Controllers/AiController.cs
│   └── Program.cs
├── AiService.Application/
│   ├── Features/Plans/Commands/GeneratePlan/
│   ├── Features/Plans/Commands/EnrichIssue/
│   ├── Features/Chat/Commands/SendMessage/
│   ├── Features/Analysis/Commands/GenerateRetrospective/
│   └── Abstractions/
│       ├── IOllamaClient.cs
│       ├── ISprintServiceClient.cs
│       ├── IIssueServiceClient.cs
│       └── IAiSessionRepository.cs
├── AiService.Domain/
│   └── Entities/
│       ├── AiSession.cs
│       └── AiMessage.cs
└── AiService.Infrastructure/
    ├── Clients/
    │   ├── OllamaClient.cs
    │   ├── SprintServiceClient.cs
    │   └── IssueServiceClient.cs
    ├── Consumers/SprintCompletedEventConsumer.cs
    ├── Persistence/AiDbContext.cs
    └── Repositories/AiSessionRepository.cs
```

---

## Ollama Prompt Stratejisi

**System prompt (plan oluşturma):**
```
Sen bir yazılım proje yöneticisisin. Kullanıcının proje tanımına göre
sprint ve issue planı oluştur. YALNIZCA geçerli JSON döndür.

Format:
{
  "sprints": [
    {
      "name": "Sprint 1",
      "goal": "...",
      "issues": [
        { "title": "...", "description": "...", "priority": "High|Medium|Low", "storyPoints": 3 }
      ]
    }
  ]
}
```

**JSON Parse + Retry:** Cevap geçersiz JSON ise fallback modelle bir kez daha dene.

---

## Docker Compose Eklemeleri

```yaml
ollama:
  image: ollama/ollama
  ports:
    - "11434:11434"
  volumes:
    - ollama_data:/root/.ollama

ai-api:
  build: ./src/services/ai/AiService.Api
  ports:
    - "5008:8080"
  environment:
    - Ollama__BaseUrl=http://ollama:11434
    - Ollama__Model=gemma3:4b
    - Ollama__FallbackModel=llama3.2:3b
    - SprintService__BaseUrl=http://sprint-api:8080
    - IssueService__BaseUrl=http://issue-api:8080
    - ConnectionStrings__DefaultConnection=...

ai-db:
  image: postgres:16
  ports:
    - "5439:5432"
```

---

## Frontend: AI Planner Sayfası

**Route:** `/projects/:id/ai-planner`

**Arayüz:**
- Chat baloncukları (kullanıcı + AI mesajları)
- Sol panel: geçmiş AI session listesi
- Plan oluşturulunca sprint özet kartları gösterilir
- Streaming progress göstergesi (LLM üretirken %)
- "Sprint Board'a Git" butonu

---

## Detaylı Faz Planı

| Faz | İçerik |
|-----|--------|
| AI-1 | Domain: `AiSession`, `AiMessage` entity'leri |
| AI-2 | Application: `GeneratePlanCommand` + handler, tüm interface'ler |
| AI-3 | Infrastructure: `OllamaClient` (prompt + JSON parse + retry), `SprintServiceClient`, `IssueServiceClient` |
| AI-4 | Api: `AiController`, `Program.cs`, DI kayıtları |
| AI-5 | Docker: ollama + ai-api + ai-db + Gateway route |
| AI-6 | SprintService + IssueService: internal-call middleware |
| AI-7 | Issue zenginleştirme + benzer issue endpoint'leri |
| AI-8 | RAG-lite chat endpoint |
| AI-9 | `SprintCompletedEvent` consumer + retrospektif özeti |
| AI-10 | Yük dengeleme + risk tahmini endpoint'leri |
| AI-11 | Frontend: `/ai-planner` chatbot sayfası + session geçmişi |

---

## Bağımlılık Sırası

```
AI-1 → AI-2 → AI-3 → AI-4 → AI-5 → AI-6
                                       ↓
                              AI-7 → AI-8 → AI-9 → AI-10 → AI-11
```

> **Not:** AI servisi Organization planı tamamlandıktan sonra başlanacak.
