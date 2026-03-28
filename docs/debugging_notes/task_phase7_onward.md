# BitirmeProject - Faz 7+ Duzeltme Plani
# Kaynak: open_issues_review_2026-03-26.md + kod taramasi

Faz 1-6 tamamlandi. Bu belge yalnizca acik kalan sorunlari kapatmak icin olusturulmus devam planini icerir.
Oncelik sirasi: open_issues_review_2026-03-26.md ile ayni.

---

## FAZ 7 - Attachment Akisinin Server-Side Orchestration'a Alinmasi

**Hedef:** Frontend'in yuruttukleri `upload -> attach -> finalize` zincirini backend'e tasi.
Orphan blob riski ve distributed transaction boslugunu kapat.

- [x] 7.1 `AttachFileCommand` kontratini sadece `fileId` tabanli yap
      - `FileName`, `ContentType`, `SizeBytes` alanlarini komuttan cikar
      - `UploadedByUserId` body'den alinmasin; Claims'ten turetilsin (handler icinde HttpContext)
      - Dosya metadata'sini handler icinde StorageService HTTP client ile doldur
      - Kaynak: `IssueService.Application/Features/Issues/Commands/AttachFile/AttachFileCommand.cs`
                `IssueService.Application/Features/Issues/Commands/AttachFile/AttachFileCommandValidator.cs`

- [x] 7.2 `AttachFileCommandHandler` icine StorageService lookup ekle
      - Handler: StorageService'e `GET /api/v1/storage/files/{fileId}` cagrisi yap
      - Dosya `Finalized` durumunda degilse komut reddet (hata don)
      - Yukaridaki lookup'tan gelen metadata ile `IssueAttachment` olustur
      - Kaynak: `IssueService.Application/Features/Issues/Commands/AttachFile/AttachFileCommandHandler.cs`

- [x] 7.3 `IssueAttachment` icin `(IssueId, FileId)` unique index ekle
      - `IssueDbContext.cs` icinde composite HasIndex + IsUnique tanimla
      - Handler'da duplicate girisimini anlamli domain hatasiyla karsilik ver
      - Kaynak: `IssueService.Infrastructure/Persistence/IssueDbContext.cs`
      - Migration olustur

- [x] 7.4 Frontend attachment akisini sadeleştir
      - `IssueDetailPanel.tsx`: `upload -> finalize -> attach` siralamasi yerine
        `upload -> attach` (handler finalize+lookup yapacak) veya
        `upload -> finalize -> attach (sadece fileId)` icin istek sadelessin
      - `UploadedByUserId` body'den gonderilmesini kaldir
      - Kaynak: `src/frontend/web/src/features/issues/IssueDetailPanel.tsx` (satirlar ~380-425)
      - Mock API guncelle: `src/frontend/web/src/api/mock.ts`

- [x] 7.5 Attachment endpoint entegrasyon testi (manuel yapilacak)
      - Gecersiz fileId, finalize edilmemis dosya ve duplicate attach senaryolarini dene
      - Kaynak: `tests/StorageService.UnitTests/` ve `IssueService` test projeleri

---

## FAZ 8 - Projection / Domain Siniri Netleştirme

**Hedef:** `IssueBoardItem` ve `SprintIssue` siniflari domain entity olmaktan ciksin;
read-side projection oldugu acikca belirtilsin veya ayri katmana tasinsin.

### 8A - IssueBoardItem

- [x] 8A.1 `IssueBoardItem`'i domain'den cikar
      - Mevcut konum: `IssueService.Domain/Entities/IssueBoardItem.cs`
      - Ya `IssueService.Infrastructure/Projections/` klasorune tasi
        ya da `IssueService.Application/ReadModels/` altinda DTO/read-model olarak tanimla
      - Domain metotlarini (`ApplyFrom`, `AssignToSprint`, `RemoveFromSprint`) kaldir;
        bu logic event handler'larda kalmali

- [x] 8A.2 `IssueDbContext` ve repository'yi yeni konuma gore guncelle
      - `IssueDbContext.cs`: DbSet<IssueBoardItem> yapilandirmasini gozden gecir
      - `IssueBoardRepository.cs`: read-only repository pattern uygula (sadece query, SaveChanges yok)
      - Kaynak: `IssueService.Infrastructure/Repositories/IssueBoardRepository.cs`

- [x] 8A.3 `IssueBoardItem` kullanan query handler'lari guncelle
      - Degisen namespace/import'lari duzelt
      - Gerekirse migration olustur (tablo adi degismiyorsa schema degisiklik yok)

### 8B - SprintIssue

- [x] 8B.1 `SprintIssue`'yu domain entity olmaktan cikar
      - Mevcut konum: `SprintService.Domain/Entities/SprintIssue.cs`
      - `SprintService.Infrastructure/Projections/` veya `Application/ReadModels/` altina tasi
      - Domain metotlarini (`UpdateStatus`, `AssignToSprint`, `RemoveFromSprint`) kaldir

- [x] 8B.2 `SprintDbContext` ve ilgili repository'yi guncelle
      - `SprintDbContext.cs`: DbSet<SprintIssue> yapilandirmasini guncelle
      - `SprintIssueRepository`'yi read-only yap veya event handler'larda direkt
        DbContext kullan (repository abstraction gereksizse kaldir)

- [x] 8B.3 `AddIssue` race condition'ini gider
      - Mevcut: `AddIssueCommandHandler` SprintIssue bulamazsa aninda `NotFoundException` firlatiyor
      - Cozum A: IssueService'e HTTP dogrulama - `SprintIssue` yoksa IssueService'e sor,
        varsa pending olarak olustur ve handler icinde yeniden dene
      - Cozum B: `AddIssueCommandHandler` eksik projection durumunda retry queue'ya al
      - Oneri: Cozum A daha basit; IssueService zaten var, HTTP call kabul edilebilir
      - Kaynak: `SprintService.Application/Features/Sprints/Commands/AddIssue/AddIssueCommandHandler.cs`
                `SprintService.Api/Events/Handlers/IssueCreatedEventHandler.cs`

---

## FAZ 9 - NotificationService HTTP Enrichment Bagimliligini Kaldir

**Hedef:** `CommentAddedEventHandler` ve `IssueStatusChangedEventHandler` icindeki
IssueService HTTP cagrilarini ortadan kaldir.

- [x] 9.1 Event contract'lari zenginlestir
      - `CommentAddedEvent`: `IssueTitle`, `AssigneeUserId`, `CreatedByUserId` alanlari eklendi
      - `IssueStatusChangedEvent`: `AssigneeUserId`, `CreatedByUserId`, `IssueTitle` alanlari eklendi
      - `AddCommentCommandHandler` ve `ChangeIssueStatusCommandHandler` publish taraflari guncellendi

- [x] 9.2 NotificationService handler'larindan HTTP client kullanimi kaldirildi
      - `CommentAddedEventHandler.cs`: HTTP call kaldirildi, payload'dan `AssigneeUserId`/`CreatedByUserId` aliniyor
      - `IssueStatusChangedEventHandler.cs`: Ayni sekilde temizlendi

- [x] 9.3 `NotificationRequestedEvent` kaldirildi
      - Hicbir servis tarafindan yayinlanmiyordu; consumer, handler, kontrakt ve DLQ kaydlari temizlendi
      - `Shared.Contracts/NotificationRequestedEvent.cs` silindi
      - `NotificationService.Api/Events/Handlers/NotificationRequestedEventHandler.cs` silindi
      - `NotificationEventsConsumer.cs` ve `NotificationDlqHealthCheck.cs` temizlendi

- [x] 9.4 NotificationService'in IssueService HttpClient registration'i kaldirildi
      - `Program.cs`: `AddHttpClient("IssueService", ...)`, `ServiceEndpoints` konfigurasyon kaydi temizlendi
      - `IssueDto.cs` ve `ServiceEndpoints.cs` model dosyalari silindi
      - `appsettings.json` ve `appsettings.Development.json` dosyalarindan `ServiceEndpoints` section'i kaldirildi
      - Unit testler HTTP mock'lardan temizlendi; `NotificationRequestedEventHandlerTests.cs` silindi

---

## FAZ 10 - ProjectService Query Tarafini Membership Bazli Yap

**Hedef:** `GetProjectsByUser` ve paged varyantin sonuclari artik sadece owner degil,
kullanicinin member oldugu tum projeleri kapsamali.

- [x] 10.1 `ProjectRepository`'ye membership bazli sorgu ekle
      - `GetByOwnerUserIdAsync` → `GetByMemberUserIdAsync` olarak degistirildi
      - `GetByOwnerUserIdPagedAsync` → `GetByMemberUserIdPagedAsync` olarak degistirildi
      - Query: `ProjectMembers` subquery ile `UserId == userId` olan tum projeleri getiriyor
      - IProjectRepository ve ProjectRepository guncellendi

- [x] 10.2 `GetProjectsByUserQueryHandler` handler'i guncellendi
      - `GetByOwnerUserIdAsync` → `GetByMemberUserIdAsync` kullaniliyor

- [x] 10.3 `GetProjectsByUserPagedQueryHandler` handler'i guncellendi
      - `GetByOwnerUserIdPagedAsync` → `GetByMemberUserIdPagedAsync` kullaniliyor

- [x] 10.4 Frontend ve mock API guncellendi
      - `mock.ts`: `getByUser` ve `getByUserPaged` artik `getProjectMembers()` JOIN ile userId'ye gore filtreliyor
      - `p.ownerUserId === userId` kontrolu kaldirildi; membership bazli filtreleme yapiliyor

---

## FAZ 11 - Identity Session Bütünlüğü: Refresh / Revoke / Lifecycle Events

**Hedef:** Kimlik dogrulama akisindaki bosluklan kapat.
Refresh endpoint ekle, logout'ta token revoke yap, lifecycle eventleri yayinla.

### 11A - Refresh Endpoint

- [x] 11A.1 `AuthController`'a refresh endpoint eklendi
      - `POST /api/v1/identity/refresh` (AllowAnonymous)
      - HTTP-only cookie'den refreshToken okunuyor
      - `RefreshTokenCommand` MediatR uzerinden gonderiliyor

- [ ] 11A.2 Frontend'e refresh akisi ekle (opsiyonel / sonradan)
      - 401 alindigi durumda otomatik refresh denemesi
      - Kaynak: `src/frontend/web/src/api/`

### 11B - Logout Token Revoke

- [x] 11B.1 `AuthController.Logout` endpoint'i gercek revoke yapiyor
      - Cookie'den refreshToken aliniyor, `RevokeTokenCommand` gonderiliyor
      - Cookie'ler siliniyor

- [x] 11B.2 `RevokeTokenCommand` + `RevokeTokenCommandHandler` olusturuldu
      - Kaynak: `IdentityService.Application/Features/Auth/Commands/Revoke/`
      - Handler: token'i hash'leyip DB'den bulur, `Revoke()` cagirir, SaveChanges

### 11C - Identity Lifecycle Events

- [x] 11C.1 `UserCreated` event yayin altyapisi kuruldu
      - `Shared.Contracts/UserCreatedEvent.cs` olusturuldu
      - IdentityService.Application'a Shared.Abstractions + Shared.Contracts referansi eklendi
      - `IdentityDbContext`'e `DbSet<OutboxMessage>` eklendi
      - `OutboxRepository` olusturuldu (Infrastructure/Repositories)
      - `IOutboxRepository` DI'a kaydedildi
      - `RegisterCommandHandler`'a outbox yazimi eklendi (UserCreatedEvent)
      - `Program.cs`'e `AddRabbitMQ` eklendi; appsettings'e RabbitMQ config eklendi
      - Migration: `20260327000000_AddOutboxMessages`

- [ ] 11C.2 `UserDeactivated` ve `UserRoleChanged` event'lerini ekle
      - `Shared.Contracts/` altinda kontratlari tanimla
      - Tetikleyici: admin kullanici deaktive ettiginde / rol degisikliginde yayinla

- [ ] 11C.3 Diger servislerin bu event'leri dinleyip dinlemeyecegine karar ver
      - En azindan `NotificationService` icin `UserCreated` dinlemesi faydali olabilir
      - Gereksiz servisler icin subscribe etme

---

## FAZ 12 - StorageService Authorization Netleştirme

**Hedef:** Dosya erisim kontrolunun parent entity (issue/project) baglamiyla iliskilendirilmesini
resmi olarak tanimla veya mevcut siniri dokumante et.

- [x] 12.1 Authorization modeli Secenek A olarak kararlaştirildi ve belgelendi

  **Secenek A uygulandı:**
  - `StorageController`: XML doc comment ile authorization siniri acikca belgelendi.
    "Download/Delete: only uploader or Admin. Parent-entity auth is IssueService's responsibility."
  - `StoredFile`: entity doc comment ile "UploadedByUserId only; parent context lives in IssueService" eklendi.
  - Secenek B (parent ref) eklenmedi: fazladan migration + complexity; IssueService zaten
    AttachFileCommandHandler'da dosya sahipligini dogruluyor.

- [x] 12.2 Secilen secenek uygulandi
      - Kaynak: `StorageService.Domain/Entities/StoredFile.cs`
                `StorageService.Api/Controllers/StorageController.cs`

---

## FAZ 13 - Sprint Velocity Immutable Snapshot

**Hedef:** Tamamlanmis sprint icin velocity/summary bilgisini immutable sekilde kaydet.

- [x] 13.1 `SprintSummary` entity tanimlandi
      - Alanlar: `SprintId` (PK), `TotalIssues`, `CompletedIssues`, `SnapshotTakenAt`, `CompletedAt`
      - Kaynak: `SprintService.Domain/Entities/SprintSummary.cs`
      - `ISprintSummaryRepository` + `SprintSummaryRepository` olusturuldu

- [x] 13.2 `CompleteSprintCommandHandler`'a snapshot kayit eklendi
      - `sprintIssues` (carry-over oncesi durum) uzerinden total/completed sayilari hesaplaniyor
      - `SprintSummary` atomik olarak ayni SaveChanges'ta yaziliyor

- [x] 13.3 `GetSprintVelocityQueryHandler` snapshot'tan besleniyor
      - Sprint.Status == Completed: snapshot'tan oku (IsSnapshot=true, SnapshotTakenAt dolu)
      - Sprint aktifse canli hesapla
      - Snapshot yoksa (Faz 13 oncesi tamamlanmis sprint) canli hesaba dusuyor
      - `SprintVelocityDto`'ya `IsSnapshot` ve `SnapshotTakenAt` alanlari eklendi

- [x] 13.4 Migration olusturuldu
      - `20260327001000_AddSprintSummary`
      - `SprintDbContextModelSnapshot` guncellendi

---

## FAZ 14 - Build / Validation Altyapisi

**Hedef:** Kapsamli `dotnet build` dogrulamasini calisir hale getir.

- [ ] 14.1 `BitirmeProject.sln` dosyasini duzelt veya `BitirmeProject.slnx`'i standart hale getir
      - Mevcut: `BitirmeProject.sln` 0 byte (bos/gecersiz)
      - Alternatif: `BitirmeProject.slnx` 4353 byte (yeni format)
      - Secenekler:
        A) `.sln` dosyasini tum projeleri icine alacak sekilde yeniden olustur
        B) `.slnx`'i resmi build entrypoint olarak CI/CD veya README'de dokumante et
      - Kaynak: `BitirmeProject.sln`, `BitirmeProject.slnx`

- [ ] 14.2 `dotnet build` ile tum servislerin derlenebildigini dogrula
      - Her servis icin ayri `dotnet build` calistir; hatalari duzelt

- [ ] 14.3 Mevcut unit test projelerini `dotnet test` ile dene
      - `tests/NotificationService.UnitTests/`
      - `tests/StorageService.UnitTests/`

---

## Oncelik ve Uygulanma Sirasi

| Faz | Baslik | Kritiklik | Bagimlilik |
|-----|--------|-----------|-----------|
| **7** | Attachment akisi server-side orchestration | KRITIK | Yok |
| **8** | IssueBoardItem + SprintIssue projection ayrimi | YUKSEK | Yok |
| **9** | NotificationService HTTP enrichment kaldir | YUKSEK | Faz 9.1 event kontratlari once |
| **10** | ProjectService membership query | ORTA | Yok |
| **11A** | Identity refresh endpoint | YUKSEK | Yok |
| **11B** | Identity logout revoke | YUKSEK | 11A sonrasi |
| **11C** | Identity lifecycle events | ORTA | 11A-11B sonrasi |
| **12** | StorageService authorization netlesme | ORTA | Faz 7 sonrasi tercih edilir |
| **13** | Sprint velocity snapshot | DUSUK | Faz 8B sonrasi tercih edilir |
| **14** | Build altyapisi | ORTA | Bagimsiz, istediginizde |

---

## Tamamlanan Fazlar (Referans)

- Faz 1: Foundation / Messaging Reliability (tamamlandi)
- Faz 2: Trust & Security Corrections (tamamlandi)
- Faz 3: Ownership & Boundary Cleanup (tamamlandi)
- Faz 4: Domain Modeling Fixes (4.7 haric tamamlandi - Faz 8'e tasindi)
- Faz 5: Attachment & Notification Lifecycle Stabilization (tamamlandi)
- Faz 6: Production Hardening & Observability (tamamlandi)
