# BitirmeProject — Hoca Sunumu
**Tarih:** 27 Mart 2026

---

## 1. Projeye Genel Bakış

**BitirmeProject**, yazılım ekipleri için kurumsal düzeyde bir **proje ve görev yönetim platformudur.**

Jira / Linear gibi araçlara benzer özellikler sunarken; **mikroservis mimarisi, olay güdümlü programlama (event-driven), CQRS ve Clean Architecture** gibi ileri düzey yazılım mühendisliği pratiklerini uygulamalı olarak içermektedir.

### Temel İşlevler

| Modül | Özellikler |
|-------|-----------|
| **Proje Yönetimi** | Proje oluşturma, arşivleme, takım üyesi yönetimi (Owner / Lead / Member) |
| **Görev (Issue) Takibi** | Oluşturma, atama, durum geçişi, yorum ve dosya ekleri |
| **Kanban Board** | Sürükle-bırak destekli görsel görev panosu |
| **Sprint Planlama** | Sprint oluşturma, backlog yönetimi, velocity hesaplama |
| **Gerçek Zamanlı Bildirim** | SignalR WebSocket ile anlık bildirim iletimi |
| **Dosya Yönetimi** | Güvenli iki aşamalı yükleme (geçici → kalıcı) |
| **Rol Tabanlı Erişim** | Admin / Owner / Lead / Member rolleriyle yetkilendirme |
| **Denetim Kaydı** | Tüm durum değişikliklerinin geçmişi |

---

## 2. Sistem Mimarisi

```
               React Frontend  (Port 5173)
                       │  HTTP / WebSocket
         API Gateway — YARP  (Port 5000)
         JWT doğrulama · CORS · Yönlendirme
           │       │      │      │      │      │      │
        Identity  Proje  Issue  Sprint  Bildir. Depo  BFF
        :5001    :5002  :5003  :5004   :5005  :5007  :5006
           │       │      │      │      │      │
                      RabbitMQ (Port 5672)
               Exchange: bitirme_events (Topic, Durable)
                              │
         PostgreSQL (6 ayrı DB) · Redis · Seq · SignalR
```

- **7 bağımsız mikroservis**, her biri kendi veritabanı ve sorumluluğu ile
- Servisler birbirini doğrudan çağırmak yerine **mesaj kuyruğu (RabbitMQ) üzerinden haberleşir**
- Kritik akışlarda **HTTP çağrısı** yalnızca zorunlu durumlarda yapılır (örn. dosya metadata doğrulaması)

---

## 3. Kullanılan Teknolojiler

### Backend

| Teknoloji | Versiyon | Amaç |
|-----------|----------|------|
| .NET / ASP.NET Core | 9.0 | Tüm mikroservislerin çalışma zamanı |
| Entity Framework Core | 9.0 | ORM, Code-First migration |
| PostgreSQL | 16 | Her servis için ayrı veritabanı |
| MediatR | 14 | CQRS pipeline (Command / Query) |
| FluentValidation | 12 | Komut doğrulama |
| RabbitMQ | 3.13 | Asenkron servisler arası iletişim |
| Redis | 7 | Dağıtık önbellekleme |
| YARP | — | API Gateway / Reverse Proxy |
| SignalR | — | Gerçek zamanlı WebSocket bildirimleri |
| Serilog + Seq | — | Yapısal loglama ve merkezi log yönetimi |
| Polly | — | HTTP retry politikaları |

### Frontend

| Teknoloji | Versiyon | Amaç |
|-----------|----------|------|
| React | 19.2 | UI bileşen kütüphanesi |
| TypeScript | 5.9 | Tip güvenli JavaScript |
| Vite | 7.3 | Geliştirme ortamı ve bundler |
| TanStack Query | 5.90 | Sunucu durum yönetimi |
| Zustand | 5 | Global auth / tema durumu |
| @microsoft/signalr | 10 | Gerçek zamanlı bağlantı |
| @dnd-kit | — | Kanban sürükle-bırak |

### Altyapı

| Teknoloji | Amaç |
|-----------|------|
| Docker + Docker Compose | Tüm servislerin konteynerleştirme ve orkestrasyon yönetimi |
| PostgreSQL 16-alpine | Hafif üretim kalitesi veritabanı imajları |
| RabbitMQ 3.13-management | Mesaj kuyruğu + yönetim arayüzü |

---

## 4. Mikroservisler

### 4.1 Identity Service (Port: 5001)
Kullanıcı kimlik doğrulama ve yetkilendirme.

- **Domain:** `User`, `Role`, `RefreshToken`, `UserRole`
- 5 başarısız girişten sonra 15 dakika **hesap kilitleme**
- Şifre / rol değişikliklerinde **SecurityStamp** yenileme → eski token'ları geçersiz kılar
- Refresh token **SHA-256 hash** ile saklanır
- JWT erişim token'ı 60 dakika geçerli

### 4.2 Project Service (Port: 5002)
Proje oluşturma ve takım yönetimi.

- **Domain:** `Project`, `ProjectMember`, `ProjectSummary` (read model)
- Üye rolleri: **Owner / Lead / Member**
- Proje sahibi aynı zamanda otomatik member olarak kaydedilir
- `IssueCreatedEvent`, `IssueStatusChangedEvent`, `SprintCompletedEvent` tüketir → ProjectSummary günceller

### 4.3 Issue Service (Port: 5003)
Görev yönetiminin ana servisi.

- **Domain:** `Issue`, `IssueComment`, `IssueAttachment`, `IssueAudit`, `IssueBoardItem`
- `If-Match` / `X-Expected-Version` header'ları ile **optimistic locking**
- Geçerli durum geçişlerini zorlayan **durum geçiş motoru** (Open → InProgress → Done)
- Dosya ekleri: `fileId` tabanlı, StorageService'ten metadata doğrulaması
- `(IssueId, FileId)` unique index ile tekrarlı ek koruması

### 4.4 Sprint Service (Port: 5004)
Sprint planlaması ve velocity hesaplama.

- **Domain:** `Sprint`, `SprintIssue`
- Tamamlanan sprint **değiştirilemez** (immutable)
- Bir projede aynı anda **yalnızca bir aktif sprint** (DB constraint)
- Sprint kapanınca tamamlanmamış issue'lar için **carry-over politikası**

### 4.5 Notification Service (Port: 5005)
Bildirim oluşturma, teslim ve gerçek zamanlı iletim.

- **Domain:** `Notification` — durum: `Queued / Sent / Delivered / Failed`
- **SignalR Hub:** `/hubs/notifications` → anlık push bildirimi
- `NotificationDeliveryWorker`: exponential backoff ile başarısız teslimat yeniden denemeleri
- `IssueAssignedEvent`, `CommentAddedEvent`, `IssueStatusChangedEvent`, `MemberAddedEvent` tüketir

### 4.6 Storage Service (Port: 5007)
Dosya yükleme ve yönetimi.

- **Domain:** `StoredFile` — durum: `Temporary / Finalized`
- **İki aşamalı yükleme:** Upload → (kullanıma bağlanınca) Finalize
- Orphan blob temizlik job'ı: finalize edilmemiş geçici dosyaları temizler
- Yalnızca dosyayı yükleyen kullanıcı veya admin indirebilir / silebilir

### 4.7 BFF (Backend for Frontend) (Port: 5006)
Frontend için özelleştirilmiş sorgu toplulaştırıcı.

---

## 5. Mimari Desenler

| Desen | Açıklama |
|-------|----------|
| **Clean Architecture** | Domain / Application / Infrastructure / API katman ayrımı; her servis bağımsız |
| **CQRS** | Komutlar (Command) ve sorgular (Query) ayrı handler'larla işlenir |
| **Outbox Pattern** | Event yayınları veritabanı transaction'ı içinde önce Outbox tablosuna yazılır, ardından arka planda RabbitMQ'ya iletilir → at-least-once garantisi |
| **Inbox Pattern** | Tüketici servislerde tekrarlı event işleme (`ProcessedEvent` kaydı ile idempotency) |
| **Domain Events** | Servis içi olay bildirimi EF Core interceptor'ları ile yönetilir |
| **Optimistic Locking** | `Version` alanı ve `If-Match` header'ı ile eş zamanlı güncelleme çakışması koruması |
| **Saga (Orchestration)** | Dosya ekleme akışı artık server-side'da yönetilir; frontend yalnızca `fileId` gönderir |
| **Delivery Worker** | Bildirim teslim yeniden denemeleri ayrı arka plan servisi üzerinden çalışır |

---

## 6. Mesajlaşma Sistemi

```
Servis A                 RabbitMQ                Servis B
   │                        │                       │
   ├─ Outbox'a yaz ─────────┤                       │
   ├─ OutboxPublisher yayın ─► Exchange ──────────► Queue B
   │                        │                       ├─ InboxEntry kontrol
   │                        │                       ├─ Handler çalıştır
   │                        │                       └─ Atom. olarak kaydet
```

- **Exchange:** `bitirme_events` (Topic, Durable)
- **Subscriber-specific queue naming:** Her tüketici servise özel queue
- **DLQ (Dead Letter Queue):** Başarısız mesajlar otomatik yönlendirilir
- **CorrelationId / ActorId** header standardı: Tüm event'lerde izlenebilirlik

---

## 7. Güvenlik Mimarisi

```
İstek → YARP Gateway → JWT doğrulama → Mikroservis
                                           │
                                    Claims'ten UserId / Role al
                                    (body'den asla güvenilmeyen veri)
```

- Tüm kimlik bilgisi **JWT Claims'ten** türetilir; body / query param'dan alınmaz
- Email ve kullanıcı adı **normalize** edilir (LowerInvariant)
- Refresh token **SHA-256 hash** ile saklanır (düz metin asla)
- Dosya download/delete için **sahiplik doğrulaması**
- Bildirim sorgularında **claims-based userId eşleşmesi**

---

## 8. Gözlemlenebilirlik (Observability)

| Özellik | Uygulama |
|---------|----------|
| Yapısal Loglama | Serilog → Seq — her log kaydında `correlationId`, `actorId`, `entityId`, `eventId`, `consumerName` |
| Sağlık Kontrolleri | Outbox worker sağlığı, DLQ derinliği, başarısız teslimat sayısı |
| Event şema versiyonu | Tüm integration event'lerde `EventVersion` alanı |
| Projection replay | Mesajları yeniden tüketerek projeksiyon yeniden oluşturma stratejisi tasarlandı |

---

## 9. Test Yapısı

| Test Projesi | Kapsam |
|--------------|--------|
| `NotificationService.UnitTests` | Komut handler'ları, consumer davranışları (CreateNotificationCommandHandler, IssueAssignedEventHandler, NotificationRequestedEventHandler) |
| `StorageService.UnitTests` | Controller testleri, FinalizeFileCommandHandler |

Test yaklaşımı: **birim testler** + endpoint bazlı **manuel entegrasyon testleri**

---

## 10. Proje İlerleme Durumu

### Tamamlanan Fazlar

| Faz | Başlık | Durum |
|-----|--------|-------|
| **1** | Foundation / Messaging Reliability | ✅ Tamamlandı |
| **2** | Trust & Security Corrections | ✅ Tamamlandı |
| **3** | Ownership & Boundary Cleanup | ✅ Tamamlandı |
| **4** | Domain Modeling Fixes | ✅ Tamamlandı (4.7 → Faz 8'e taşındı) |
| **5** | Attachment & Notification Lifecycle Stabilization | ✅ Tamamlandı |
| **6** | Production Hardening & Observability | ✅ Tamamlandı |
| **7** | Attachment Akışının Server-Side Orchestration'a Alınması | ✅ Tamamlandı |

**Faz 1–7 içeriğinde tamamlanan başlıca işler:**
- RabbitMQ topology, Outbox/Inbox pattern, DLQ, CorrelationId standardı
- JWT Claims tabanlı kimlik doğrulama, refresh token hashing, hesap kilitleme
- Sprint immutability, aktif sprint unique constraint, carry-over politikası
- Bildirim teslimat yaşam döngüsü (Queued → Sent → Delivered / Failed) ve delivery worker
- Dosya yükleme iki aşamalı akış + orphan cleanup job
- Yapısal loglama + sağlık kontrolleri + projection replay stratejisi
- Attachment komutunu `fileId` tabanlı hale getirme; metadata StorageService'ten alınıyor

---

### Devam Eden / Planlanan Fazlar

| Faz | Başlık | Öncelik |
|-----|--------|---------|
| **8** | `IssueBoardItem` ve `SprintIssue` projection katmanına taşıma | Yüksek |
| **9** | NotificationService'te IssueService HTTP bağımlılığını kaldır (event contract zenginleştirme) | Yüksek |
| **10** | ProjectService: üye bazlı proje sorgusu (yalnızca owner değil, tüm üyeler) | Orta |
| **11** | Identity: refresh endpoint, logout revoke, lifecycle event'leri | Yüksek |
| **12** | StorageService: yetkilendirme modelini netleştir veya parent-entity bağlamı ekle | Orta |
| **13** | Sprint tamamlanınca immutable velocity snapshot kaydı | Düşük |
| **14** | Build altyapısı: `.sln` düzeltme, `dotnet build` / `dotnet test` doğrulama | Orta |

---

## 11. Tipik Kullanıcı Akışı

```
1. Kullanıcı kayıt / giriş → JWT + Refresh Token alır
2. Proje oluşturur → üye ekler (Owner/Lead/Member rolüyle)
3. Issue oluşturur → sprint'e ekler
4. Issue durumu güncellenir → atanan kişiye SignalR bildirimi gider
5. Dosya yükler → StorageService'e gönderilir (Temporary)
6. Issue'ya dosya ekler → handler StorageService'e danışır,
   dosyayı Finalize eder ve IssueAttachment kaydeder
7. Sprint tamamlanır → carry-over politikası çalışır,
   velocity hesaplanır, ilgili event'ler yayınlanır
```

---

## 12. Özet

**Proje, 7 bağımsız mikroservisten oluşan, event-driven, CQRS tabanlı bir proje yönetim platformudur.**

- **Mimari:** Clean Architecture + CQRS + Outbox/Inbox Pattern + Optimistic Locking
- **Haberleşme:** RabbitMQ (async) + YARP Gateway (HTTP proxy)
- **Güvenlik:** JWT Claims-based, SHA-256 refresh token, hesap kilitleme
- **Gözlemlenebilirlik:** Serilog, Seq, health check endpoint'leri
- **Frontend:** React 19 + TypeScript + SignalR gerçek zamanlı bildirimler
- **Test:** Unit testler + manuel entegrasyon testleri
- **İlerleme:** Faz 1–7 tamamlandı (temel mimari, güvenlik, domain modeli, attachment ve bildirim yaşam döngüleri, production hardening); Faz 8–14 devam planında
