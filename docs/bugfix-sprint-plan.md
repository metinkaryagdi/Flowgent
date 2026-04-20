# Bug Fix Sprint Planı
_Tarih: 2026-04-19 | Branch: feature/organization-invite_

Öncelik sırası: P0 (production blocker) → P1 (yüksek risk) → P2 (stabilite & hardening)

---

## P0 — Production Blocker

| ID | Dosya(lar) | Yapılacak | Bitti? |
|----|-----------|-----------|--------|
| P0-1 | `src/gateway/ApiGateway/Program.cs` | CORS: `SetIsOriginAllowed(_ => true)` kaldır, whitelist'e al, config'den oku | [ ] |
| P0-2 | `IssueService.Api/Events/SprintEventsConsumer.cs` + 3 consumer | ExistsAsync→HandleAsync→ProcessedEvent atomik transaction'a al, unique index ekle | [ ] |
| P0-3 | `IdentityService.Infrastructure/Persistence/AdminUserSeeder.cs` + `Program.cs` | Prod'da seeder'ı kapat, sabit parolayı env var'a taşı | [ ] |
| P0-4 | `SprintsController.cs` + `GetBacklogQueryHandler.cs` + `SprintIssueRepository.cs` | Backlog sorgusuna `OrganizationId == currentOrgId` scope ekle | [ ] |
| P0-5 | `docker-compose.yml` + tüm `appsettings*.json` | JWT secret, RabbitMQ, DB şifreleri env secret'a taşı, `.env.example` bırak | [ ] |
| P0-6 | `ApiGateway/Program.cs` + `AuthController.cs` + davet controller'ları | IP bazlı rate limiting, login/refresh/invite için ayrı policy | [ ] |

### P0 Dosya Detayları

**P0-1 CORS** (`ApiGateway/Program.cs` satır 24-26):
- `SetIsOriginAllowed(_ => true)` var → kaldır
- `AllowCredentials()` var → whitelist ile birlikte kullan
- Prod ve dev origin'lerini `appsettings.json`'dan oku
- Test: rastgele origin'den credential'lı istek → 403

**P0-2 Consumer atomicity** (4 consumer):
- `ExistsAsync → HandleAsync → AddAsync → SaveChangesAsync` transaction dışında
- Tüm adımları `BeginTransactionAsync` ile sar
- `ProcessedEvent(EventId, EventType)` için unique index ekle (migration)
- Test: aynı event iki kez deliver → tek kez işlenmeli

**P0-3 Admin seeder** (`AdminUserSeeder.cs` satır 14-16):
- `admin@bitirme.local` / `Admin@123` hardcoded
- Prod'da seeder'ı env flag ile kapat (`SEED_ADMIN=false`)
- Dev için bile env var zorunlu yap: `ADMIN_PASSWORD`
- İlk admin bootstrap'ı ayrı CLI komutuna taşı

**P0-4 Backlog cross-org** (`SprintsController` + `GetBacklogQueryHandler`):
- Controller sadece `projectId` gönderiyor → `organizationId` ekle
- Handler'da org scope kontrolü yok → `OrganizationId == currentOrgId` filtre
- Repository sorgusuna org filtresi ekle
- Test: başka tenant projectId ile backlog isteği → 403/boş

**P0-5 Secrets** (`docker-compose.yml`):
- JWT secret: `"YourSuperSecretKey..."` 6+ serviste hardcoded
- RabbitMQ: `admin:admin123`, Redis: `redis123`, DB şifreleri açık
- `.env.example` yaz, gerçek değerleri `.env`'e taşı (gitignore'da olmalı)
- `appsettings.json`'da kalan secret'ları env var referansına çevir

**P0-6 Rate limiting** (`ApiGateway/Program.cs`):
- Auth endpoint'lerinde hiç rate limiting yok
- Gateway'de IP bazlı limiter ekle
- `/auth/login`, `/auth/refresh` → sıkı policy
- `/invites/validate`, `/auth/register` → ayrı policy
- Test: 429 response

---

## P1 — Yüksek Risk

| ID | Dosya(lar) | Yapılacak | Bitti? |
|----|-----------|-----------|--------|
| P1-1 | `ApiGateway/Program.cs` | Org header'larını önce sil, sonra tek değer olarak set et | [ ] |
| P1-2 | `useSignalR.ts` + `authStore.ts` | SignalR'ı cookie auth ile bağla, localStorage token beklentisini kaldır | [ ] |
| P1-3 | `api/client.ts` + `RefreshTokenCommandHandler.cs` | BroadcastChannel ile multi-tab koordinasyonu, backend grace window | [ ] |
| P1-4 | `InternalServiceMiddleware.cs` (tüm servisler) + `AiService appsettings` | Caller whitelist, key maskeleme, internal surface inventory | [ ] |
| P1-5 | `AiService.Application/...` prompt handler'ları | Input sanitize helper, uzunluk limiti, system/user prompt ayrımı | [ ] |
| P1-6 | `OllamaClient.cs` + `AiService/Program.cs` | Timeout, Polly retry/circuit breaker, semaphore concurrency limiti | [ ] |
| P1-7 | `docker-compose.yml` + `AiService appsettings` | Ollama'yı compose servisi yap veya `extra_hosts` ile Linux uyumlu hale getir | [ ] |

### P1 Dosya Detayları

**P1-1 Header spoof** (`ApiGateway/Program.cs` satır 77, 80):
- `TryAddWithoutValidation` kullanıyor — client gönderirse üstüne eklenir
- Önce `Request.Headers.Remove("X-Organization-Id")` + `Remove("X-Organization-Role")`
- Sonra `Request.Headers.Add(...)` ile güvenilir değeri set et

**P1-2 SignalR auth** (`useSignalR.ts` satır 13):
- `localStorage.getItem('accessToken')` → XSS riskli
- Cookie tabanlı auth kullan: `withCredentials: true`
- Ya da hub için kısa ömürlü token endpoint tasarla
- Test: login sonrası notification bağlantısı stabil kurulmalı

**P1-3 Multi-tab refresh** (`api/client.ts`):
- `failedQueue` sadece aynı tab koordinasyonu
- `BroadcastChannel('auth_refresh')` ekle
- Backend'de kısa grace window veya reuse-detection
- Test: iki tab aynı anda expire → kullanıcı logout olmamalı

**P1-4 Internal middleware** (`InternalServiceMiddleware.cs`):
- Key doğrulanıyor ama başarılı/başarısız call'lar loglanmıyor
- Caller isimlerini whitelist et (header'dan oku)
- Key'i maskeli logla (ilk 4 karakter + `****`)
- Internal endpoint surface'ini inventory çıkar

**P1-5 AI prompt injection** (prompt handler'ları):
- Kullanıcı girdisi ham prompt'a gömülüyor
- Sanitize helper: özel karakter escape, max uzunluk (500 char)
- System prompt ile user content ayrımı: `[USER_DATA]...[/USER_DATA]`

**P1-6 AI dayanıklılık** (`OllamaClient.cs`):
- `HttpClient` timeout yok, circuit breaker yok
- `HttpClient.Timeout = TimeSpan.FromSeconds(30)` ekle
- Polly: 3 retry (exponential backoff) + circuit breaker
- `SemaphoreSlim(5)` ile max concurrency

**P1-7 Ollama Linux uyumu** (`docker-compose.yml`):
- `host.docker.internal` Linux'ta kırılabilir
- Seçenek A: Ollama'yı compose servisi olarak ekle
- Seçenek B: `extra_hosts: - "host.docker.internal:host-gateway"` ekle
- Base URL'yi `OLLAMA_BASE_URL` env var ile yönet

---

## P2 — Stabilite & Hardening

| ID | Dosya(lar) | Yapılacak | Bitti? |
|----|-----------|-----------|--------|
| P2-1 | Tüm servislerin `Program.cs` | Migration'ı ayrı init job'a taşı veya distributed lock uygula | [ ] |
| P2-2 | `Shared.Common/Messaging/...` + RabbitMQ health check | DLQ sayacı, outbox pending age metriği, alert eşiği, health endpoint | [ ] |
| P2-3 | `README.md` + `docs/requests.http` | Cookie bazlı auth akışını dokümana geçir, token kopyalama akışını kaldır | [ ] |
| P2-4 | `tests/` altı | 6 senaryo için security regression testi | [ ] |

### P2-4 Security Regression Test Senaryoları
1. Başka origin'den credential'lı istek → reddedilmeli
2. Duplicate event replay → tek kez işlenmeli
3. Başka org project backlog erişimi → 403
4. Multi-tab refresh yarışı → logout olmamalı
5. Default admin hesabı yok → 404/401
6. Internal header spoof denemesi → downstream güvenilir değeri görmeli

---

## Uygulama Sırası

```
P0-1 → P0-2 → P0-3 → P0-4 → P0-5 → P0-6
→ P1-1 → P1-2 → P1-3 → P1-4 → P1-5 → P1-6 → P1-7
→ P2-1 → P2-2 → P2-3 → P2-4
```

## Doğrulama Özeti (2026-04-19)

Tüm dosyalar incelendi, sorunlar kod seviyesinde doğrulandı:
- `SetIsOriginAllowed(_ => true)` + `AllowCredentials()` → satır 24-26
- 4 consumer'da atomik transaction yok → satır 116-185 arası
- `Admin@123` hardcoded → satır 14-16
- Backlog'da org scope yok → handler'da sadece projectId
- JWT secret `"YourSuperSecretKey..."` compose'da 6+ serviste
- `localStorage.getItem('accessToken')` → useSignalR.ts satır 13
- `BroadcastChannel` yok → api/client.ts
- OllamaClient timeout/circuit breaker yok
