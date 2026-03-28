# BitirmeProject — Proje Tanıtım Dokümanı
**Hazırlanma Tarihi:** 27 Mart 2026

---

## İçindekiler

1. [Projeye Genel Bakış](#1-projeye-genel-bakış)
2. [Sistem Mimarisi](#2-sistem-mimarisi)
3. [Kullanılan Teknolojiler](#3-kullanılan-teknolojiler)
4. [Mikroservisler ve Sorumlulukları](#4-mikroservisler-ve-sorumlulukları)
5. [Paylaşılan Kütüphaneler](#5-paylaşılan-kütüphaneler)
6. [Frontend Yapısı](#6-frontend-yapısı)
7. [Altyapı ve DevOps](#7-altyapı-ve-devops)
8. [Veritabanı Tasarımı](#8-veritabanı-tasarımı)
9. [Mesajlaşma Sistemi ve Event Akışı](#9-mesajlaşma-sistemi-ve-event-akışı)
10. [Kimlik Doğrulama ve Yetkilendirme](#10-kimlik-doğrulama-ve-yetkilendirme)
11. [Mimari Desenler](#11-mimari-desenler)
12. [Test Yapısı](#12-test-yapısı)
13. [Gözlemlenebilirlik: Loglama ve Sağlık Kontrolleri](#13-gözlemlenebilirlik-loglama-ve-sağlık-kontrolleri)
14. [Tipik Kullanıcı Akışı](#14-tipik-kullanıcı-akışı)
15. [Proje Geliştirme Süreci ve Fazlar](#15-proje-geliştirme-süreci-ve-fazlar)

---

## 1. Projeye Genel Bakış

**BitirmeProject**, yazılım ekiplerinin proje ve görev yönetimini yapabildiği, gerçek zamanlı bildirimler alabileceği, sprint planlaması yapabileceği ve dosya ekleri yönetebileceği **kurumsal düzeyde bir proje yönetim platformudur.**

Proje; modern yazılım geliştirme ekiplerinin kullandığı Jira, Linear gibi araçlara benzer özellikler sunarken, **mikroservis mimarisi, olay güdümlü programlama (event-driven), CQRS ve Clean Architecture** gibi ileri düzey yazılım mühendisliği pratiklerini uygulamalı olarak içermektedir.

### Temel Özellikler

| Özellik | Açıklama |
|---------|----------|
| Proje Yönetimi | Proje oluşturma, arşivleme, takım üyesi yönetimi (Owner/Lead/Member rolleri) |
| Görev (Issue) Takibi | Görev oluşturma, atama, durum değiştirme, yorum ve dosya ekleri |
| Kanban Board | Sürükle-bırak destekli görsel görev panosu |
| Sprint Planlama | Sprint oluşturma, backlog yönetimi, velocity hesaplama |
| Gerçek Zamanlı Bildirimler | SignalR WebSocket ile anlık bildirim iletimi |
| Dosya Yönetimi | Güvenli dosya yükleme, geçici → kalıcı hale getirme akışı |
| Rol Tabanlı Erişim | Admin, Owner, Lead, Member rolleriyle yetkilendirme |
| Denetim Kaydı | Tüm durum değişikliklerinin geçmişini saklama |

---

## 2. Sistem Mimarisi

Sistem, her birinin kendi veritabanına ve sorumluluğuna sahip olduğu **7 bağımsız mikroservis** ile bunları birbirine bağlayan paylaşılan kütüphaneler ve altyapı bileşenlerinden oluşmaktadır.

```
                          ┌───────────────────────────────────────────┐
                          │         React Frontend  (Port 5173)        │
                          └───────────────────┬───────────────────────┘
                                              │ HTTP / WebSocket
                          ┌───────────────────▼───────────────────────┐
                          │      API Gateway - YARP  (Port 5000)       │
                          │  JWT doğrulama · CORS · Yönlendirme        │
                          └──┬───┬────┬────┬────┬────┬────┬───────────┘
                             │   │    │    │    │    │    │
               ┌─────────────▼┐ ┌▼──┐ ┌▼──┐ ┌▼──┐ ┌▼──┐ ┌▼──┐ ┌▼───┐
               │  Identity    │ │Prj│ │Iss│ │Spr│ │Not│ │Sto│ │BFF │
               │  :5001       │ │:5002│ │:5003│ │:5004│ │:5005│ │:5007│ │:5006│
               └─────┬────────┘ └─┬─┘ └─┬─┘ └─┬─┘ └─┬─┘ └─┬─┘ └────┘
                     │            │   │    │    │    │
              ┌──────▼────────────▼───▼────▼────▼────▼─────────────────┐
              │                 RabbitMQ  (Port 5672)                    │
              │     Exchange: bitirme_events  (Topic, Durable)           │
              └─────────────────────────────────────────────────────────┘
                     │
              ┌──────▼────────────────────────────────────────────────┐
              │   PostgreSQL (6 ayrı DB) · Redis · Seq · SignalR       │
              └───────────────────────────────────────────────────────┘
```

---

## 3. Kullanılan Teknolojiler

### Backend

| Teknoloji | Versiyon | Kullanım Amacı |
|-----------|----------|----------------|
| **.NET / ASP.NET Core** | 9.0 | Tüm mikroservislerin çalışma zamanı |
| **Entity Framework Core** | 9.0.11 | ORM, Code-First migration, veritabanı erişimi |
| **PostgreSQL** | 16 | Her mikroservis için ayrı ilişkisel veritabanı |
| **MediatR** | 14.0.0 | CQRS pipeline (Command/Query işleyicileri) |
| **FluentValidation** | 12.1.1 | Komut ve sorgu doğrulama |
| **AutoMapper** | 16.0.0 | Entity → DTO dönüşümleri |
| **RabbitMQ** | 3.13 | Asenkron mikroservis iletişimi (AMQP protokolü) |
| **Redis** | 7 | Dağıtık önbellekleme |
| **YARP** | — | API Gateway / Reverse Proxy |
| **SignalR** | — | Gerçek zamanlı WebSocket bildirimleri |
| **Serilog** | — | Yapısal loglama |
| **Seq** | 2024.3 | Merkezi log yönetimi ve görselleştirme |
| **Polly** | — | Dayanıklı HTTP istemcisi (retry politikaları) |
| **Npgsql** | 9.0.4 | PostgreSQL için .NET sürücüsü |

### Frontend

| Teknoloji | Versiyon | Kullanım Amacı |
|-----------|----------|----------------|
| **React** | 19.2.0 | UI bileşen kütüphanesi |
| **TypeScript** | 5.9.3 | Tip güvenli JavaScript |
| **Vite** | 7.3.1 | Hızlı geliştirme ortamı ve bundler |
| **React Router** | 7.13.1 | SPA yönlendirme |
| **Zustand** | 5.0.11 | Global durum yönetimi (auth, tema) |
| **TanStack Query** | 5.90.21 | Sunucu durum yönetimi, önbellekleme |
| **Axios** | 1.13.6 | HTTP istemcisi |
| **@microsoft/signalr** | 10.0.0 | Gerçek zamanlı WebSocket bağlantısı |
| **@dnd-kit** | — | Kanban board için sürükle-bırak |

### Altyapı ve DevOps

| Teknoloji | Kullanım Amacı |
|-----------|----------------|
| **Docker** | Konteynerleştirme |
| **Docker Compose** | Tüm servislerin orkestrasyon yönetimi |
| **PostgreSQL 16-alpine** | Hafif, üretim kalitesi veritabanı imajları |
| **RabbitMQ 3.13-management** | Mesaj kuyruğu + yönetim arayüzü |

---

## 4. Mikroservisler ve Sorumlulukları

### 4.1 Identity Service (Port: 5001)

Kullanıcı kimlik doğrulama ve yetkilendirmeden sorumludur. Sistemin güvenlik katmanının merkezidir.

**Domain Varlıkları:**
- `User`: Kullanıcı bilgileri, şifre hash, hesap kilitleme sayacı, güvenlik damgası
- `Role`: Sistem rolleri
- `RefreshToken`: Oturum yenileme token'ları (hash'lenmiş, soft-revoke)
- `UserRole`: Kullanıcı-rol ilişki tablosu

**API Uç Noktaları (`/api/v1/identity`):**
```
POST /register        → Yeni kullanıcı kaydı
POST /login           → Giriş (JWT + Refresh Token → HttpOnly Cookie)
POST /logout          → Çıkış
GET  /users           → Kullanıcı listesi (admin)
GET  /roles           → Rol listesi
```

**Güvenlik Özellikleri:**
- 5 başarısız girişten sonra 15 dakika hesap kilitleme
- Şifre değişikliği, rol değişikliği ve durum değişikliklerinde `SecurityStamp` yenileme (eski token'ları geçersiz kılar)
- Refresh token SHA-256 ile hash'lenmiş olarak saklanır
- JWT erişim token'ı 60 dakika geçerli

---

### 4.2 Project Service (Port: 5002)

Proje oluşturma ve takım yönetiminden sorumludur.

**Domain Varlıkları:**
- `Project`: Proje adı, kısaltma (key), arşivlenme durumu
- `ProjectMember`: Kullanıcı-proje ilişkisi ve rol (Owner / Lead / Member)
- `ProjectSummary`: Proje bazlı issue sayıları (read model)

**API Uç Noktaları (`/api/v1/projects`):**
```
POST   /                       → Proje oluştur
GET    /{id}                   → Proje detayı
GET    /user/{userId}          → Kullanıcının projeleri
GET    /user/{userId}/paged    → Sayfalı, arama/arşiv filtreli
GET    /{id}/members           → Takım üyeleri
POST   /{id}/members           → Üye ekle
DELETE /{id}/members/{userId}  → Üye çıkar
PUT    /{id}                   → Proje güncelle
DELETE /{id}                   → Proje sil
```

**Tükettiği Event'ler:**
- `IssueCreatedEvent` → ProjectSummary güncelleme
- `IssueStatusChangedEvent` → Durum bazlı sayaçlar
- `SprintCompletedEvent` → Sprint metrikleri

**Yayınladığı Event'ler:** `ProjectCreatedEvent`, `ProjectUpdatedEvent`, `MemberAddedEvent`

---

### 4.3 Issue Service (Port: 5003)

Görev yönetiminin ana servisidir. Tüm issue CRUD işlemleri, yorumlar, dosya ekleri ve durum geçişleri bu serviste yönetilir.

**Domain Varlıkları:**
- `Issue`: Başlık, açıklama, durum (Open/InProgress/Done), öncelik (Low/Medium/High/Critical), optimistic locking için `Version` alanı
- `IssueComment`: Yorum içeriği ve yazarı
- `IssueAttachment`: Dosya referansı (StorageService'teki `fileId`)
- `IssueAudit`: Durum değişiklik geçmişi
- `IssueBoardItem`: Kanban board için denormalize read model

**API Uç Noktaları (`/api/v1/issues`):**
```
POST   /                          → Issue oluştur
GET    /{id}                      → Issue detayı
GET    /project/{projectId}       → Projenin tüm issue'ları
GET    /project/{projectId}/paged → Sayfalı, sprint/backlog filtreli
GET    /assignee/{userId}         → Atanmış issue'lar
GET    /sprint/{sprintId}         → Sprint'teki issue'lar
GET    /{id}/history              → Denetim kaydı
GET    /workflow                  → Geçerli durum geçişleri
POST   /{id}/assign               → Issue ata (If-Match header ile)
POST   /{id}/comments             → Yorum ekle
POST   /{id}/attachments          → Dosya ekle (fileId bazlı)
GET    /{id}/attachments          → Dosya listesi
POST   /{id}/status               → Durum değiştir (If-Match header ile)
```

**Özel Özellikler:**
- `If-Match` / `X-Expected-Version` header'ları ile **optimistic locking** (eş zamanlı güncelleme koruması)
- **Durum geçiş motoru**: Yalnızca geçerli geçişlere izin verir (Open→InProgress, InProgress→Done vb.)
- `StorageService`'e servis-içi HTTP çağrısıyla dosya metadata'sı doğrulaması

**Yayınladığı Event'ler:** `IssueCreatedEvent`, `IssueStatusChangedEvent`, `IssueAssignedEvent`, `CommentAddedEvent`

---

### 4.4 Sprint Service (Port: 5004)

Sprint planlaması, backlog yönetimi ve velocity hesaplamasından sorumludur.

**Domain Varlıkları:**
- `Sprint`: Başlık, hedef, başlangıç/bitiş tarihi, durum (Planned/Active/Completed), carry-over politikası
- `SprintIssue`: Sprint'e atanmış issue'ların takibi

**API Uç Noktaları (`/api/v1/sprints`):**
```
POST   /                        → Sprint oluştur
GET    /active/{projectId}      → Aktif sprint
GET    /{id}                    → Sprint detayı
GET    /{id}/issues             → Sprint issue'ları
GET    /{id}/velocity           → Velocity metrikleri
GET    /project/{id}/backlog    → Backlog (sprint'e atanmamış)
POST   /{id}/start              → Sprint başlat (Planned → Active)
POST   /{id}/complete           → Sprint tamamla (Active → Completed)
POST   /{id}/issues             → Issue ekle
DELETE /{id}/issues/{issueId}   → Issue çıkar
```

**İş Kuralları:**
- Tamamlanan sprint değiştirilemez (immutable)
- Bir proje için yalnızca bir aktif sprint olabilir (DB constraint)
- Sprint tamamlanınca tamamlanmamış issue'lar için carry-over politikası uygulanır

**Yayınladığı Event'ler:** `SprintStartedEvent`, `SprintCompletedEvent`, `IssueAddedToSprintEvent`, `IssueRemovedFromSprintEvent`

---

### 4.5 Notification Service (Port: 5005)

Kullanıcı bildirimlerinin oluşturulması, teslim edilmesi ve gerçek zamanlı iletiminden sorumludur.

**Domain Varlıkları:**
- `Notification`: Başlık, mesaj, kanal (InApp/Email/Push), durum (Queued/Sent/Delivered/Failed), teslimat sayacı, sonraki deneme zamanı, hata açıklaması

**API Uç Noktaları (`/api/v1/notifications`):**
```
GET    /user/{userId}   → Kullanıcı bildirimleri (sayfalı)
GET    /{id}            → Bildirim detayı
POST   /{id}/read       → Okundu işaretle
DELETE /cleanup         → Eski bildirimleri temizle
```

**SignalR Hub:** `/hubs/notifications`
- `ReceiveNotification`: Yeni bildirim iletimi
- `NotificationRead`: Okunma bildirimi

**Tükettiği Event'ler:**
- `IssueAssignedEvent` → Atanan kişiye bildirim
- `IssueStatusChangedEvent` → İlgili kişilere bildirim
- `CommentAddedEvent` → Issue katılımcılarına bildirim
- `MemberAddedEvent` → Yeni üyeye hoş geldin bildirimi

**Arka Plan Servisleri:**
- `NotificationDeliveryWorker`: Exponential backoff ile başarısız teslimat yeniden denemeleri
- `NotificationDeliveryMonitor`: Teslimat sağlık kontrolü

---

### 4.6 Storage Service (Port: 5007)

Dosya yükleme, saklama ve yönetiminden sorumludur. Bilinçli olarak **saf blob + minimum metadata** servisi olarak tasarlanmıştır.

**Domain Varlıkları:**
- `StoredFile`: Dosya adı, içerik tipi, boyut, yükleyen kullanıcı, durum (Temporary/Finalized)

**API Uç Noktaları (`/api/v1/storage`):**
```
POST   /files              → Dosya yükle (geçici)
GET    /files/{id}         → Dosya metadata'sı
GET    /files/{id}/content → Dosya içeriği indir
POST   /files/{id}/finalize → Geçiciyi kalıcı hale getir
DELETE /files/{id}         → Dosya sil
```

**İki Aşamalı Yükleme Akışı:**
1. Frontend → `POST /files` → Geçici dosya oluşturulur
2. Frontend → `POST /files/{id}/finalize` → Dosya kalıcı hale gelir
3. IssueService → `GET /files/{id}` → Dosyanın Finalized olduğunu doğrular, metadata'yı alır

**Arka Plan Servisleri:**
- `StorageOrphanCleanupService`: Hiçbir issue'ya bağlanmamış geçici dosyaları periyodik olarak siler

---

### 4.7 API Gateway (Port: 5000)

Tüm dış trafiğin tek giriş noktasıdır. **YARP (Yet Another Reverse Proxy)** kullanılarak geliştirilmiştir.

**Sorumlulukları:**
- JWT token doğrulama (HttpOnly Cookie veya Authorization header'dan)
- CORS yönetimi (frontend origin izni)
- Path bazlı yönlendirme (her servis için ayrı route)
- Correlation ID propagasyonu

**Yönlendirme Tablosu:**

| Yol | Hedef Servis |
|-----|-------------|
| `/api/v1/identity/**` | identity-api:8080 |
| `/api/v1/projects/**` | project-api:8080 |
| `/api/v1/issues/**` | issue-api:8080 |
| `/api/v1/sprints/**` | sprint-api:8080 |
| `/api/v1/notifications/**` | notification-api:8080 |
| `/api/v1/storage/**` | storage-api:8080 |
| `/api/v1/bff/**` | bff-api:8080 |

---

### 4.8 BFF — Backend for Frontend (Port: 5006)

Birden fazla servisten veri toplayarak frontend'in ihtiyaçlarına özel yanıtlar üretir.

**Uç Noktaları:**
```
GET /api/v1/bff/flags  → Frontend UI özellik bayrakları
```

**Özellikler:**
- Polly ile 3 tekrarlı retry politikası (200ms × deneme sayısı exponential backoff)
- Birden fazla servis çağrısını birleştirip tek yanıt döndürür

---

## 5. Paylaşılan Kütüphaneler

### 5.1 Shared.Abstractions

Tüm mikroservislerin temel aldığı sözleşmeler ve soyutlamalar:

- **`Entity<TId>`**: Tüm domain varlıklarının temel sınıfı (Id, CreatedAt, UpdatedAt, IsDeleted)
- **`AggregateRoot<TId>`**: Domain event yönetimi
- **`IIntegrationEvent`**: Event sözleşmesi (EventId, OccurredOn, CorrelationId, EventVersion)
- **`IEventBus`**: Event yayın ve abonelik arayüzü
- **`IEventHandler<TEvent>`**: Event işleyici arayüzü
- **`OutboxMessage`**: Outbox pattern için mesaj modeli
- **`IOutboxRepository` / `IInboxRepository`**: Outbox/inbox persistans arayüzleri
- **`CorrelationContext`**: İstek kapsamlı correlation ID bağlamı
- **Exception hiyerarşisi**: `DomainException`, `NotFoundException`, `ValidationException`, `BusinessRuleException`, `ConcurrencyException`

### 5.2 Shared.Common

Tüm servislerin kullandığı ortak implementasyonlar:

- **`RabbitMQEventBus`**: `IEventBus` implementasyonu — RabbitMQ ile event yayın/tüketim
- **`OutboxPublisherService`**: Outbox pattern arka plan servisi
- **`OutboxPublisherHealthCheck`**: Outbox worker sağlık kontrolü
- **`CorrelationIdMiddleware`**: Correlation ID middleware
- **`ServiceCollectionExtensions`**: `AddRabbitMQ()` DI extension metodu
- **`IntegrationEventMetadata`**: Event metadata yardımcısı
- Serilog log enrichment yardımcıları

### 5.3 Shared.Contracts

Servisler arasında dolaşan tüm integration event kontratlari:

| Event | Yayınlayan | Tüketenler |
|-------|-----------|-----------|
| `IssueCreatedEvent` | IssueService | ProjectService, SprintService, NotificationService |
| `IssueStatusChangedEvent` | IssueService | ProjectService, SprintService, NotificationService |
| `IssueAssignedEvent` | IssueService | ProjectService, NotificationService |
| `CommentAddedEvent` | IssueService | NotificationService |
| `IssueAddedToSprintEvent` | SprintService | IssueService |
| `IssueRemovedFromSprintEvent` | SprintService | IssueService |
| `SprintStartedEvent` | SprintService | ProjectService |
| `SprintCompletedEvent` | SprintService | ProjectService, IssueService |
| `ProjectCreatedEvent` | ProjectService | — |
| `MemberAddedEvent` | ProjectService | NotificationService |
| `NotificationRequestedEvent` | Herhangi servis | NotificationService |
| `NotificationCreatedEvent` | NotificationService | — (audit) |

---

## 6. Frontend Yapısı

### Teknoloji Seçimleri

React 19 tabanlı Single Page Application. TypeScript ile tip güvenliği sağlanmıştır.

### Durum Yönetimi

- **Zustand**: Kimlik doğrulama durumu (kullanıcı, roller, bayraklar) ve tema
- **TanStack Query**: Sunucu verisi önbellekleme ve senkronizasyon

### Sayfa ve Bileşenler

| Sayfa | Açıklama |
|-------|----------|
| `LoginPage` / `RegisterPage` | Kimlik doğrulama formları |
| `ProjectsPage` | Proje listesi ve oluşturma |
| `ProjectDetailPage` | Proje detay, üye yönetimi |
| `IssuesPage` + `IssueDetailPanel` | Issue listesi ve detay paneli |
| `BoardPage` | Sürükle-bırak Kanban panosu |
| `SprintPage` | Sprint yönetimi ve backlog |
| `NotificationsPage` | Bildirim merkezi |
| `AdminPage` | Yönetici paneli |

### Rota Koruması

`ProtectedRoute` bileşeni ile kimlik doğrulanmamış kullanıcılar giriş sayfasına yönlendirilir. `AdminRoute` ile yalnızca admin rolündeki kullanıcılar ilgili sayfalara erişebilir.

### Gerçek Zamanlı Bildirimler

`useSignalR` hook'u ile SignalR WebSocket bağlantısı kurulur. Bağlantı kesilmesinde 0ms / 2s / 5s / 10s / 30s exponential backoff ile yeniden bağlanır.

### Mock API

Geliştirme ortamında `VITE_USE_MOCK_API=true` ile gerçek backend olmadan tüm arayüz test edilebilir. LocalStorage tabanlı mock veri katmanı ile proje, issue, sprint, bildirim ve yorum işlemleri simüle edilir.

### API Katmanı

- Axios örneği `withCredentials: true` ile yapılandırılmıştır (HttpOnly cookie desteği)
- 401 yanıtında otomatik `/login` yönlendirmesi
- API Base URL: `http://localhost:5000` (Gateway)

---

## 7. Altyapı ve DevOps

### Docker Compose

Tüm sistem tek bir `docker-compose.yml` dosyasıyla ayağa kaldırılır:

```bash
docker-compose up -d
```

**Servis Başlangıç Sırası (depends_on):**
```
Altyapı (RabbitMQ, Redis, Seq, 6x PostgreSQL)
        ↓
Mikroservisler (sağlık kontrolü geçince)
        ↓
Gateway + Frontend
```

### Ağ Topolojisi

Tüm konteynerler `bitirme-net` adlı ortak Docker bridge ağında çalışır. Dış dünyaya yalnızca tanımlı portlar açıktır:

| Servis | Dış Port | İç Port |
|--------|---------|---------|
| Gateway | 5000 | 8080 |
| IdentityService | 5001 | 8080 |
| ProjectService | 5002 | 8080 |
| IssueService | 5003 | 8080 |
| SprintService | 5004 | 8080 |
| NotificationService | 5005 | 8080 |
| BFF | 5006 | 8080 |
| StorageService | 5007 | 8080 |
| Frontend | 5173 | 5173 |
| RabbitMQ AMQP | 5672 | 5672 |
| RabbitMQ Yönetim | 15672 | 15672 |
| Seq UI | 5341 | 80 |
| Redis | 6379 | 6379 |

### Kalıcı Veri Hacimleri (Volumes)

Her servisin veritabanı, RabbitMQ mesaj kuyruğu, Seq logları ve storage dosyaları container yeniden başlamalarında kaybolmamak için Docker named volumes ile saklanır.

---

## 8. Veritabanı Tasarımı

**Her mikroservis kendi PostgreSQL veritabanına sahiptir.** Servisler birbirinin veritabanına doğrudan erişemez; yalnızca event'ler aracılığıyla veri paylaşılır.

### Identity Veritabanı (identitydb)

| Tablo | Temel Sütunlar |
|-------|----------------|
| `Users` | Id, UserName (unique), Email (unique), PasswordHash, Status, FailedLoginCount, LockoutEnd, SecurityStamp |
| `Roles` | Id, Name |
| `UserRoles` | UserId, RoleId |
| `RefreshTokens` | Id, UserId, Token (hash, unique), ExpiresAt, RevokedAt |

### Project Veritabanı (projectdb)

| Tablo | Temel Sütunlar |
|-------|----------------|
| `Projects` | Id, Name, Key, OwnerUserId, IsArchived |
| `ProjectMembers` | Id, ProjectId, UserId, Role (Owner/Lead/Member) |
| `ProjectSummaries` | ProjectId, TotalIssues, OpenIssues, InProgressIssues, DoneIssues |
| `OutboxMessages` | Outbox pattern mesajları |
| `ProcessedEvents` | İşlenmiş event kayıtları (idempotency) |

### Issue Veritabanı (issuedb)

| Tablo | Temel Sütunlar |
|-------|----------------|
| `Issues` | Id, ProjectId, Title, Description, Status, Priority, CreatedByUserId, AssigneeUserId, **Version** |
| `IssueComments` | Id, IssueId, Content, AuthorUserId |
| `IssueAttachments` | Id, IssueId, FileId, FileName, ContentType, SizeBytes, UploadedByUserId · Unique index: (IssueId, FileId) |
| `IssueAudits` | Id, IssueId, FromStatus, ToStatus, ChangedByUserId, ChangedAt |
| `IssueBoardItems` | IssueId (PK), Title, Status, Priority, ProjectId, SprintId — Kanban read model |
| `OutboxMessages` | Outbox |
| `ProcessedEvents` | Idempotency |

### Sprint Veritabanı (sprintdb)

| Tablo | Temel Sütunlar |
|-------|----------------|
| `Sprints` | Id, ProjectId, Name, Goal, StartDate, EndDate, Status, CarryOverPolicy · Unique: (ProjectId, Status=Active) |
| `SprintIssues` | SprintId, IssueId, Title, Status, Priority |

### Notification Veritabanı (notificationdb)

| Tablo | Temel Sütunlar |
|-------|----------------|
| `Notifications` | Id, UserId, Title, Message, Channel, **Status**, IsRead, ReadAt, DeliveryAttemptCount, NextDeliveryAttemptAt, LastFailureReason |
| `OutboxMessages` | Outbox |
| `ProcessedEvents` | Idempotency |

### Storage Veritabanı (storagedb)

| Tablo | Temel Sütunlar |
|-------|----------------|
| `StoredFiles` | Id, FileName, ContentType, SizeBytes, StoragePath, UploadedByUserId, **Status** (Temporary/Finalized), ExpiresAt, FinalizedAt |

---

## 9. Mesajlaşma Sistemi ve Event Akışı

### RabbitMQ Yapılandırması

- **Exchange:** `bitirme_events` (Topic türü, kalıcı)
- **Kuyruk adlandırma:** `{EventTypeAdı}_queue` (örn. `IssueCreatedEvent_queue`)
- **Routing key:** Event tipi adı
- **Mesaj özellikleri:** Kalıcı, ContentType: application/json, özel header'lar (x-event-id, x-correlation-id, x-event-version)

### Transactional Outbox Pattern

Microservisler, RabbitMQ'ya doğrudan yazmaz. Bunun yerine **Transactional Outbox** deseni uygulanır:

```
1. İş mantığı çalışır (örn. issue oluşturulur)
2. Aynı veritabanı transaction'ında OutboxMessage kaydı oluşturulur
3. Transaction commit olur → Hem iş verisi hem de event kaydı güvende
4. OutboxPublisherService arka planda:
   a. 50 mesajlık batch claim eder (optimistic lock ile)
   b. Her mesajı RabbitMQ'ya yayınlar
   c. Başarılıysa "Published" olarak işaretler
   d. Başarısızsa exponential backoff (10s, 30s, 60s, 120s, 300s) ile yeniden planlar
   e. 5 denemeden sonra hata olarak kalır (Dead Letter)
```

**Garanti:** En az bir kez iletim (at-least-once delivery)

### Idempotent Event İşleme (Inbox Pattern)

Her tüketen servis, işlediği event'lerin `EventId`'sini `ProcessedEvents` tablosuna yazar. Aynı event tekrar gelirse atlanır. Bu sayede RabbitMQ'nun "en az bir kez" garantisi, sistemin tamamında **etkin bir kez** (effectively-once) işlemeye dönüşür.

### Correlation ID İzleme

Her HTTP isteği ve her event, benzersiz bir `CorrelationId` taşır. Bu ID:
- HTTP header'ları üzerinden tüm servislere iletilir
- RabbitMQ mesajlarının header'ına eklenir
- Tüm log kayıtlarına otomatik olarak dahil edilir
- Böylece dağıtık bir işlemin tüm adımları Seq'de tek sorguda izlenebilir

---

## 10. Kimlik Doğrulama ve Yetkilendirme

### JWT Token Yapısı

- **Algoritma:** HS256 (HMAC-SHA256)
- **Issuer:** `BitirmeProject.IdentityService`
- **Audience:** `BitirmeProject.Clients`
- **Süre:** 60 dakika
- **Taşıma:** HttpOnly Cookie (`accessToken`) — XSS saldırılarına karşı koruma

**Token Claims:**
```
sub   : UserId (Guid)
name  : UserName
email : Email
role  : Admin / Owner / Member (virgülle ayrılmış)
iss   : Issuer
aud   : Audience
exp   : Expiration timestamp
```

### Refresh Token Mekanizması

- SHA-256 ile hash'lenmiş token veritabanında saklanır (düz metin asla saklanmaz)
- Geçerlilik süresi: 30 gün
- Tek kullanımlık (her yenilemede token rotasyonu)
- Logout anında veritabanında revoke edilir

### Güvenlik Katmanları

| Katman | Mekanizma |
|--------|----------|
| Şifre saklama | Bcrypt hash (salt dahil) |
| Token taşıma | HttpOnly Cookie (JavaScript erişemez) |
| Hesap kilitleme | 5 başarısız girişte 15 dk kilit |
| Oturum geçersizleştirme | SecurityStamp değişikliği |
| Servis erişimi | Tüm endpoint'ler `[Authorize]` ile korunur |
| Servislerarası güven | Paylaşılan JWT secret + token forwarding |
| UploadedByUserId | Her zaman Claims'ten, body'den asla alınmaz |

### Gateway Düzeyinde Doğrulama

Tüm gelen istekler önce Gateway'de JWT doğrulamasından geçer. Cookie veya Authorization header'dan token okunur. Geçersiz token → 401 Unauthorized, downstream servise istek iletilmez.

---

## 11. Mimari Desenler

### Clean Architecture

Her mikroservis 4 katmanlı yapıya sahiptir:

```
Api (Sunum Katmanı)
  ├─ Controllers, Middleware, Event Consumers
Application (Uygulama Katmanı)
  ├─ Commands, Queries, Handlers
  ├─ DTOs, Validators (FluentValidation)
  └─ Abstractions (repository arayüzleri)
Domain (Alan Katmanı)
  ├─ Entities, AggregateRoots
  ├─ Enums, Domain Rules
  └─ Value Objects
Infrastructure (Altyapı Katmanı)
  ├─ EF Core DbContext, Repositories
  ├─ HTTP Clients (servislerarası)
  └─ DI kayıtları
```

Katmanlar arasındaki bağımlılık yalnızca içe doğrudur (Infrastructure → Application → Domain). Domain, hiçbir dışa bağımlılık içermez.

### CQRS (Command Query Responsibility Segregation)

- **Command'lar:** Durumu değiştiren işlemler (CreateIssue, ChangeStatus, AddComment...)
- **Query'ler:** Yalnızca okuma yapan işlemler (GetIssueById, GetProjectsByUser...)
- MediatR pipeline üzerinden yürütülür
- Okuma ve yazma modelleri ayrı tutulur (örn. `Issue` yazma, `IssueBoardItem` okuma)

### MediatR Pipeline

Her komut/sorgu şu adımlardan geçer:

```
Controller.Send(command)
  → ValidationBehavior (FluentValidation)
  → Handler (iş mantığı)
  → Response
```

Hata durumunda `ExceptionHandlingMiddleware` uygun HTTP yanıt kodunu döner.

### Optimistic Locking

`Issue` entity'si `Version` alanı taşır. Güncelleme yapılırken istemci bu versiyonu `If-Match` veya `X-Expected-Version` header'ıyla gönderir. Eş zamanlı iki güncelleme girişiminde yalnızca biri başarılı olur, diğeri 409 Conflict alır.

### Domain-Driven Design (DDD)

- Her mikroservis bir **Bounded Context**'e karşılık gelir
- `AggregateRoot` sınıfı domain event'lerini yönetir
- Repository soyutlamaları sayesinde domain katmanı altyapıya bağımlı değildir
- İş kuralları domain entity'lerinde kapsüllenir (örn. sprint durum geçiş kuralları)

### Event-Driven Architecture

Servisler birbirini doğrudan çağırmak yerine event'ler aracılığıyla haberleşir. Bu sayede:
- **Gevşek bağlılık (loose coupling):** Bir servisin çökmesi diğerini etkilemez
- **Ölçeklenebilirlik:** Her servis bağımsız ölçeklenebilir
- **Dayanıklılık:** Outbox pattern ile event kaybı önlenir

### Eventual Consistency

Servislerarası veri tutarlılığı event'ler üzerinden sağlanır:
- ProjectSummary sayaçları → IssueCreatedEvent tüketilince güncellenir
- IssueBoardItem → Issue event'leri üzerinden senkronize olur
- Kısa gecikmeli tutarlılık (eventual consistency) bilinçli kabul edilmiştir

---

## 12. Test Yapısı

Her servis için karşılıklı bir Unit Test projesi bulunmaktadır.

### Test Kapsamı

| Test Kategorisi | İçerik |
|-----------------|--------|
| **Command Handler Testleri** | İş mantığı doğrulama, durum değişiklikleri |
| **Query Handler Testleri** | Veri erişim mantığı, filtreleme |
| **Validator Testleri** | FluentValidation kurallarının doğruluğu |
| **Domain Model Testleri** | Entity davranışları, invariant'lar |
| **Controller Testleri** | HTTP istek/yanıt akışları |
| **Event Consumer Testleri** | Event işleme ve veri senkronizasyonu |
| **Middleware Testleri** | Correlation ID, hata yakalama |

### Kapsanan Servisler

- `IdentityService.UnitTests`
- `ProjectService.UnitTests`
- `IssueService.UnitTests`
- `SprintService.UnitTests`
- `NotificationService.UnitTests`
- `StorageService.UnitTests`
- `Bff.UnitTests`
- `Shared.UnitTests`

---

## 13. Gözlemlenebilirlik: Loglama ve Sağlık Kontrolleri

### Serilog ile Yapısal Loglama

Tüm servisler Serilog kullanır. Loglar hem konsola hem de merkezi Seq sunucusuna yazılır:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()   // CorrelationId, UserId otomatik eklenir
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();
```

**Seq Erişimi:** `http://localhost:5341`

Seq üzerinde:
- Correlation ID ile dağıtık iz takibi
- Servis bazlı filtreleme
- Hata takibi ve uyarı kuralları
- Gerçek zamanlı log akışı

### Sağlık Kontrolleri (Health Checks)

Her servis `/health` endpoint'i sunar:

| Servis | Ek Sağlık Kontrolü |
|--------|-------------------|
| NotificationService | `notification_delivery_worker`, `notification_dlq` |
| StorageService | `storage_cleanup_worker` |
| Shared (tüm servisler) | `outbox_publisher_worker` |

Docker Compose bu endpoint'leri kullanarak servislerin hazır olup olmadığını kontrol eder.

---

## 14. Tipik Kullanıcı Akışı

### Uçtan Uca Örnek: Issue Oluşturma ve Tamamlama

```
1. KAYIT / GİRİŞ
   POST /api/v1/identity/login
   → JWT Cookie set → Frontend localStorage'a kullanıcı bilgisi yazar

2. PROJE OLUŞTURMA
   POST /api/v1/projects
   → Project entity + ProjectMember(Owner) oluşturulur
   → OutboxMessage → RabbitMQ → ProjectCreatedEvent yayınlanır

3. ISSUE OLUŞTURMA
   POST /api/v1/issues  { projectId, title, priority }
   → Issue entity (Version=1) oluşturulur
   → OutboxMessage → IssueCreatedEvent
   → ProjectService: ProjectSummary.TotalIssues + 1
   → SprintService: Issue backlog'a eklenir

4. ISSUE ATAMA
   POST /api/v1/issues/{id}/assign  { assigneeUserId }
   Header: If-Match: "1"
   → Issue.AssigneeUserId güncellenir, Version: 1 → 2
   → OutboxMessage → IssueAssignedEvent
   → NotificationService: Atanan kişiye "size bir görev atandı" bildirimi
   → SignalR → Frontend real-time bildirim gösterir

5. SPRINT BAŞLATMA
   POST /api/v1/sprints/{id}/start
   → Sprint.Status: Planned → Active
   → SprintStartedEvent yayınlanır

6. ISSUE SPRİNT'E EKLEME
   POST /api/v1/sprints/{id}/issues  { issueId }
   → SprintIssue kaydı oluşturulur
   → IssueAddedToSprintEvent → IssueService: Issue.SprintId güncellenir

7. DOSYA EKLEME
   POST /api/v1/storage/files (multipart)  → geçici dosya
   POST /api/v1/storage/files/{id}/finalize → kalıcı
   POST /api/v1/issues/{id}/attachments  { fileId }
   → IssueService: StorageService'e metadata doğrulama çağrısı
   → IssueAttachment kaydı oluşturulur (IssueId, FileId unique)

8. DURUM DEĞİŞTİRME
   POST /api/v1/issues/{id}/status  { newStatus: "Done" }
   Header: If-Match: "3"
   → Workflow motoru geçişi doğrular (InProgress → Done)
   → IssueAudit kaydı oluşturulur
   → IssueStatusChangedEvent
   → ProjectService: OpenIssues -1, DoneIssues +1
   → SprintService: Velocity güncellenir
   → NotificationService: İlgililere bildirim

9. SPRİNT TAMAMLAMA
   POST /api/v1/sprints/{id}/complete
   → Sprint.Status: Active → Completed (immutable)
   → Tamamlanmamış issue'lar carry-over politikasına göre işlenir
   → SprintCompletedEvent → Tüm abonelere
```

---

## 15. Proje Geliştirme Süreci ve Fazlar

Proje, her faz öncesinde kod taraması ve sorun tespiti yapılarak 6 ana geliştirme fazını tamamlamıştır. 7. faz devam etmektedir.

| Faz | Başlık | Durum |
|-----|--------|-------|
| **Faz 1** | Foundation / Messaging Reliability — RabbitMQ topoloji, Outbox model güçlendirme, optimistic claim, Inbox/ProcessedEvent, DLQ, CorrelationId standardı | ✅ Tamamlandı |
| **Faz 2** | Trust & Security — Endpoint yetkilendirme ayrımı, Claims bazlı kimlik, email normalizasyon, refresh token hashing, StorageService sahiplik kontrolü | ✅ Tamamlandı |
| **Faz 3** | Ownership & Boundary Cleanup — Sprint assignment sahiplik kararı, read-only projection alanları, ProjectSummary read model, NotificationService politika sahipliği, StorageService sınır tanımı | ✅ Tamamlandı |
| **Faz 4** | Domain Modeling — Sprint StartDate/EndDate/Goal, aktif sprint DB constraint, tamamlanan sprint immutability, carry-over politikası, ProjectMember rolleri, Notification delivery lifecycle, User aggregate güçlendirme | ✅ Tamamlandı |
| **Faz 5** | Attachment & Notification Lifecycle — StorageService geçici yükleme + finalize akışı, orphan temizleme job'ı, Notification delivery worker, delivery state ayrımı | ✅ Tamamlandı |
| **Faz 6** | Production Hardening — Yapısal loglama standardı, health check altyapısı, event schema versiyonlama, projection replay stratejisi belgesi | ✅ Tamamlandı |
| **Faz 7** | Attachment API Server-Side Orchestration — AttachFileCommand fileId-only kontrakt, StorageService metadata lookup, duplicate attachment koruması (unique index), UploadedByUserId Claims'ten | 🔄 Devam Ediyor |

**Sıradaki Fazlar (Planlanan):**
- **Faz 8:** IssueBoardItem ve SprintIssue'nun domain'den projection katmanına taşınması
- **Faz 9:** NotificationService'teki IssueService HTTP enrichment bağımlılığının kaldırılması
- **Faz 10:** ProjectService query tarafının membership bazlı hale getirilmesi
- **Faz 11:** Identity refresh endpoint, logout token revoke, lifecycle events
- **Faz 12:** StorageService authorization model netleştirme
- **Faz 13:** Sprint velocity immutable snapshot

---

*Bu doküman proje kaynak kodu üzerinden otomatik analiz ve geliştirici notlarıyla hazırlanmıştır.*
