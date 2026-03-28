# Backend Mantıksal Hatalar ve Eksik Altyapı — 2026-03-27

Kod taraması (debugger gözüyle) sonucu tespit edilen açık hatalar ve tamamlanmamış maddeler.
Derleme hatası değil; çalışma zamanı mantık hataları ve mimari boşluklar.

---

## BÖLÜM 1: Mantıksal Hatalar (Bug)

---

### BUG 1 (KRİTİK): JwtBearer cookie'yi okumuyor → tüm API 401 döner

**Durum:** Açık
**Dosyalar:**
- `src/services/identity/IdentityService.Api/Program.cs`
- `src/services/issues/IssueService.Api/Program.cs`
- `src/services/sprints/SprintService.Api/Program.cs`
- `src/services/storage/StorageService.Api/Program.cs`
- `src/services/notifications/NotificationService.Api/Program.cs`
- `src/services/projects/ProjectService.Api/Program.cs`

**Problem:**
`AuthController.SetTokenCookies`, JWT'yi `accessToken` adlı HttpOnly cookie olarak set ediyor ve response body'den siliyor (`StripTokens → AccessToken = string.Empty`). Frontend token değerini alamıyor.

Tüm servislerin JwtBearer konfigürasyonu standart header okuma modunda; `OnMessageReceived` eventi ile cookie okuma eklenmemiş:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        // sadece standart Authorization: Bearer <token> header okuma
        // cookie okuma YOK
    });
```

JwtBearer middleware varsayılan olarak yalnızca `Authorization: Bearer <token>` header'ından okur. Login sonrası tüm servislere gelen istekler 401 Unauthorized döner — hiçbir authenticated endpoint çalışmaz.

**Fix:**
Her servisin JwtBearer options'ına `OnMessageReceived` ekle:
```csharp
options.Events = new JwtBearerEvents {
    OnMessageReceived = ctx => {
        ctx.Token = ctx.Request.Cookies["accessToken"];
        return Task.CompletedTask;
    }
};
```

---

### BUG 2 (YÜKSEK): Servisler arası çağrılarda token iletimi boş

**Durum:** Açık
**Dosyalar:**
- `src/services/issues/IssueService.Api/Controllers/IssuesController.cs` — satır ~131 (`AttachFile`)
- `src/services/sprints/SprintService.Api/Controllers/SprintsController.cs` — satır ~55 (`AddIssue`)

**Problem:**
Cookie auth kullanıldığında `Authorization` header'ı boş gelir. Her iki controller da token'ı buradan çekiyor:

```csharp
// IssuesController.AttachFile
var bearerToken = Request.Headers["Authorization"].ToString()
    .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase); // → "" (boş)

// SprintsController.AddIssue
var token = Request.Headers.Authorization.ToString()
    .Replace("Bearer ", string.Empty, ...); // → "" (boş)
```

`StorageServiceClient` ve `IssueServiceClient`, token boşsa Authorization header eklemeden istek atar:

```csharp
if (!string.IsNullOrWhiteSpace(bearerToken))  // false → header eklenmez
    client.DefaultRequestHeaders.Authorization = ...;
```

Sonuç: StorageService ve IssueService'e giden servisler arası HTTP çağrıları 401 alır.
Etkilenen akışlar: dosya ekleme (`AttachFile`), sprint'e issue ekleme (projection yoksa race condition guard).

**Önemli not:** Bug 1 çözülse bile bu hata bağımsız olarak varlığını sürdürür.
Fix yöntemi: controller'larda `Authorization` header'dan değil, `Request.Cookies["accessToken"]` cookie'sinden token okunup iletilmeli.

```csharp
// Önerilen fix yöntemi
var bearerToken = Request.Cookies["accessToken"];
```

---

### BUG 3 (ORTA): `CommentAddedEventHandler` — atanan kişi yorum yazarı ise yaratıcıya bildirim gitmiyor

**Durum:** Açık
**Dosya:** `src/services/notifications/NotificationService.Api/Events/Handlers/CommentAddedEventHandler.cs`

**Problem:**
Mevcut recipient seçim mantığı:

```csharp
var recipient = @event.AssigneeUserId ?? @event.CreatedByUserId;
if (recipient == Guid.Empty || recipient == @event.AuthorUserId)
    return;
```

`AssigneeUserId` dolu ama `AuthorUserId`'ye eşitse (yani atanan kişi yorum yazanın kendisiyse), handler `CreatedByUserId`'ye düşmeden erken çıkıyor. Issue yaratıcısı bildirim almıyor.

**Senaryo:**
- Issue → Creator: A, Assignee: B
- B yorum yazar → `AuthorUserId=B`, `AssigneeUserId=B`, `CreatedByUserId=A`
- `recipient = B`, `B == B` → return → A hiç bildirim almıyor

Kardeş handler `IssueStatusChangedEventHandler.ResolveRecipient` aynı durumu doğru şekilde ele alıyor (önce assignee dene, olmadı creator'a düş). `CommentAddedEventHandler` bu fallback mantığını kullanmıyor.

**Fix:**
```csharp
// Önce assignee dene (yazar değilse)
if (@event.AssigneeUserId.HasValue
    && @event.AssigneeUserId.Value != Guid.Empty
    && @event.AssigneeUserId.Value != @event.AuthorUserId)
{
    // AssigneeUserId'ye bildirim gönder
}
// Sonra creator'a dene (yazar değilse)
else if (@event.CreatedByUserId != Guid.Empty
         && @event.CreatedByUserId != @event.AuthorUserId)
{
    // CreatedByUserId'ye bildirim gönder
}
```

---

### BUG 4 (ORTA): `IssueServiceClient` — enum sayı olarak serialize edildiği için `SprintIssue.Priority` tutarsız

**Durum:** Açık
**Dosya:** `src/services/sprints/SprintService.Infrastructure/Clients/IssueServiceClient.cs` — satır ~49-51

**Problem:**
`IssueServiceClient`, IssueService'ten dönen JSON'ı manuel parse ediyor:

```csharp
Priority: raw.GetProperty("priority").ToString(),
Status:   raw.GetProperty("status").ToString(),
```

`IssueDto.Priority` tipi `IssuePriority` enum'u. `AddControllers()` varsayılanında System.Text.Json enumları sayı olarak serialize eder (`IssuePriority.High = 2` → `2`). `JsonElement.ToString()` sayı için `"2"` döndürür, `"High"` değil.

Ama `IssueCreatedEvent` publisher şunu yapar:
```csharp
new IssueCreatedEvent(..., issue.Priority.ToString(), ...)
// → "High"  (enum.ToString() = isim)
```

Sonuç: Aynı issue için `SprintIssue.Priority`:
- Normal yol (IssueCreatedEvent handler): `"High"`
- Race condition yolu (IssueServiceClient): `"2"`

Aynı veritabanında tutarsız string değerler oluşuyor. `Status` alanı için de aynı problem geçerli.

**Fix seçenekleri:**
- A) `IssueServiceClient`'ta `GetProperty("priority").GetInt32()` ile sayıyı al, sonra `((IssuePriority)val).ToString()` ile isme çevir
- B) IssueService'te enum'ları string olarak serialize et (`JsonStringEnumConverter`) — tüm API için tutarlı olur
- C) `IssueCreatedEvent`'ta priority/status'u enum int olarak gönder, her iki yolda da sayı kullan

---

## BÖLÜM 2: Bu Buglar Düzeltilince Yetmeyecek — Ek Eksikler

Bu bugların tamamı giderilse bile backend uçtan uca çalışmaz. Aşağıdaki maddeler de gerekli:

---

### EKSİK 1 (KRİTİK): Infrastructure servisleri ayağa kaldırılmalı

Servisler start olurken şunlara bağlanıyor; bunlar çalışmıyorsa servisler ayağa kalkmaz veya anlık çöker:

| Servis | Nerede Kullanılıyor |
|--------|---------------------|
| PostgreSQL | Her mikroservis — migration ve tüm veri işlemleri |
| RabbitMQ | Outbox publisher, event consumer'lar |
| Redis | IssueService — `AddStackExchangeRedisCache` ile zorunlu kayıt |
| Seq | Opsiyonel log aggregation (olmasa da çalışır ama hata logları kaybolur) |

**Not:** `docker-compose` ile bu servislerin ayağa kaldırılması gerekiyor. Eksik kalırsa servisler start olmaz.

---

### EKSİK 2 (YÜKSEK): Frontend refresh akışı yok (Faz 11A.2 tamamlanmadı)

**Dosya:** `src/frontend/web/src/api/` (henüz yok)

Access token expire olduğunda frontend 401 alır ama otomatik refresh denemez. Kullanıcı oturumu kopar, manuel logout/login gerekir.

**Fix:** Axios interceptor'da 401 alındığında `POST /api/v1/identity/refresh` çağrısı yapılmalı. Backend tarafı hazır (`AuthController.Refresh` endpoint mevcut), yalnızca frontend'de yapılmamış.

---

### EKSİK 3 (DÜŞÜK): `UserDeactivated` ve `UserRoleChanged` event'leri yayınlanmıyor (Faz 11C.2 tamamlanmadı)

**Dosya:** `src/services/identity/IdentityService.Application/` (komutlar eksik)

Kullanıcı deaktive edildiğinde veya rolü değiştiğinde diğer servisler bu değişikliği bilmiyor. Stale auth state oluşabilir. Temel fonksiyonellik için kritik değil ama güvenlik/tutarlılık açısından eksik.

---

## BÖLÜM 3: Kabul Edilen Sınırlar (Kasıtlı Bırakıldı)

Bunlar `open_issues_review_2026-03-26.md`'de belgelenmiş ve bilinçli kabul edilmiş:

| Sınır | Karar |
|-------|-------|
| Shared symmetric JWT secret | Güvenlik riski ama fonksiyonel — kabul edildi |
| StorageService download yalnızca uploader/admin | Parent-entity auth IssueService sorumluluğunda — kabul edildi |
| SignalR token sorunu | Frontend F4 fazına ertelendi |

---

## BÖLÜM 4: Genel Durum Tablosu

Bug'lar + eksikler giderildikten sonra beklenen uçtan uca durum:

| Akış | Durum (buglar giderilince) |
|------|---------------------------|
| Login / Register / Logout | ✅ çalışır |
| Token refresh (401 → otomatik yenile) | ❌ EKSİK 2 tamamlanmadan çalışmaz |
| Issue CRUD (oluştur, listele, detay) | ✅ çalışır |
| Issue durum değiştirme | ✅ çalışır |
| Issue atama | ✅ çalışır |
| Yorum ekleme + bildirim | ✅ (Bug 3 fix sonrası) |
| Dosya yükleme + issue'ya ekleme | ✅ (Bug 1+2 fix sonrası) |
| Kanban board projeksiyonu | ✅ çalışır |
| Sprint oluşturma / başlatma / tamamlama | ✅ çalışır |
| Sprint'e issue ekleme | ✅ (Bug 1+2 fix sonrası) |
| Velocity snapshot | ✅ çalışır |
| Proje oluşturma / listeleme (membership) | ✅ çalışır |
| Bildirim listeleme | ✅ çalışır |
| Outbox → RabbitMQ event yayını | ✅ (infrastructure varsa) |
| SignalR real-time board güncellemeleri | ❌ Frontend F4 fazına ertelendi |
| UserDeactivated event yayını | ❌ EKSİK 3 (opsiyonel) |

---

## Öncelik Sırası

| Öncelik | Madde | Tür |
|---------|-------|-----|
| 1 | Bug 1 — JwtBearer cookie okuma | KRİTİK bug |
| 2 | Bug 2 — Servisler arası token iletimi | YÜKSEK bug |
| 3 | EKSİK 1 — Infrastructure (docker-compose) | KRİTİK altyapı |
| 4 | Bug 3 — CommentAdded bildirim fallback | ORTA bug |
| 5 | Bug 4 — SprintIssue Priority tutarsızlığı | ORTA bug |
| 6 | EKSİK 2 — Frontend refresh interceptor | YÜKSEK, frontend tarafı |
| 7 | EKSİK 3 — UserDeactivated event | DÜŞÜK, opsiyonel |
