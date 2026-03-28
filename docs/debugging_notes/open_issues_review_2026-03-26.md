# Kalan Sorunlar Kaydi - 26 Mart 2026

Bu belge, `docs/debugging_notes` altindaki notlar ile mevcut kodun karsilastirilmasi sonucunda halen acik veya kismi kalan sorunlari kaydeder. Tamamlanan fazlar burada tekrar listelenmez; burada sadece kapanmamis veya tam oturmamis basliklar vardir.

## Genel Sonuc

- Faz 1-2 ve 5-6 tarafinda ciddi iyilesme var.
- Buna ragmen notlardaki tum mimari ve davranissal riskler kapanmis degil.
- En kritik kalan alanlar: issue attachment akisi, projection/domain ayrimi, notification enrichment coupling, owner-centric project query modeli ve identity session/lifecycle bosluklari.

## 1. IssueService

### 1.1 `IssueBoardItem` hala domain icinde

Sorun:
`IssueBoardItem`, projection/read-side yerine hala domain entity gibi tutuluyor. Bu, notlardaki "domain'den cikar" beklentisinin kapanmadigini gosteriyor.

Neden acik:
- `IssueService.Domain/Entities/IssueBoardItem.cs`
- `IssueService.Infrastructure/Persistence/IssueDbContext.cs`
- `IssueService.Infrastructure/Repositories/IssueBoardRepository.cs`

Etki:
- Write model ile read model siniri bulanik kaliyor.
- Projection drift ve manuel sync maliyeti devam ediyor.
- `task.md` icindeki `4.7` maddesi gercekte hala acik.

Onerilen sonraki adim:
`IssueBoardItem` sinifini domain katmanindan cikarip projection/read-side katmanina tasi. Query handler'lari ve repository soyutlamalarini buna gore ayir.

### 1.2 Attachment akisi hala client-driven

Sorun:
Frontend hala `upload -> issue attach -> finalize` zincirini kendisi yurutuyor. Bu, notlarda isaretlenen distributed transaction ve orphan riskini tamamen kapatmiyor.

Neden acik:
- `src/frontend/web/src/features/issues/IssueDetailPanel.tsx`
- `src/services/issues/IssueService.Api/Controllers/IssuesController.cs`
- `src/services/storage/StorageService.Api/Controllers/StorageController.cs`

Etki:
- Aradaki herhangi bir adim fail olursa gecici blob, eksik attachment kaydi veya finalize edilmemis dosya kalabiliyor.
- Orchestration istemciye itildigi icin davranis merkezi ve guvenilir degil.

Onerilen sonraki adim:
Attachment olusturma ve finalize surecini tek bir server-side orchestration akisina topla. Frontend sadece niyet belirtmeli, transaction/saga karari backend'de olmali.

### 1.3 Attachment API kontrati mevcut frontend ile uyusmuyor

Sorun:
Frontend attachment olustururken yalnizca `fileId` gonderiyor. Buna karsin `AttachFileCommand` hala `FileName`, `ContentType`, `SizeBytes`, `UploadedByUserId` bekliyor.

Neden acik:
- `src/frontend/web/src/features/issues/IssueDetailPanel.tsx`
- `src/services/issues/IssueService.Application/Features/Issues/Commands/AttachFile/AttachFileCommand.cs`
- `src/services/issues/IssueService.Application/Features/Issues/Commands/AttachFile/AttachFileCommandValidator.cs`

Etki:
- Mevcut API kontrati kirik veya tesadufen calisan bir hale geliyor.
- `UploadedByUserId` gibi alanlar body'den geldigi icin guven sorunu da suruyor.

Onerilen sonraki adim:
Issue attachment komutunu `fileId` tabanli hale getir. Dosya metadata'sini StorageService veya guvenilir server-side lookup ile doldur. `UploadedByUserId` body'den alinmasin.

### 1.4 Duplicate attachment korumasi eksik

Sorun:
`IssueAttachment` icin `(IssueId, FileId)` bazli unique constraint yok. Sadece ayri ayri index'ler var.

Neden acik:
- `src/services/issues/IssueService.Infrastructure/Persistence/IssueDbContext.cs`

Etki:
- Ayni dosya ayni issue'ya birden fazla kez baglanabilir.

Onerilen sonraki adim:
`IssueAttachment` tablosuna `(IssueId, FileId)` unique index ekle ve command seviyesinde anlamli hata don.

## 2. NotificationService

### 2.1 IssueService'e HTTP enrichment bagimliligi devam ediyor

Sorun:
Notification handler'lari hala IssueService'e HTTP cagrisi yapip eksik bilgiyi oradan tamamliyor.

Neden acik:
- `src/services/notifications/NotificationService.Api/Program.cs`
- `src/services/notifications/NotificationService.Api/Events/Handlers/CommentAddedEventHandler.cs`
- `src/services/notifications/NotificationService.Api/Events/Handlers/IssueStatusChangedEventHandler.cs`

Etki:
- Event contract zayif kalmaya devam ediyor.
- Transient HTTP hatalari notification kaybi veya gecikmesine yol acabiliyor.
- Servis siniri gevsek kaliyor, distributed monolith kokusu suruyor.

Onerilen sonraki adim:
Notification kararini cikarmak icin gereken recipient/payload alanlarini event contract'a tasi veya NotificationService icinde projection/read model kur.

### 2.2 `NotificationRequestedEvent` yolu kodda hala duruyor

Sorun:
NotificationService icinde command-benzeri `NotificationRequestedEvent` hattinin contract, consumer ve handler kodu hala mevcut.

Neden acik:
- `src/shared/contracts/Shared.Contracts/NotificationRequestedEvent.cs`
- `src/services/notifications/NotificationService.Api/Events/Handlers/NotificationRequestedEventHandler.cs`
- `src/services/notifications/NotificationService.Api/Events/NotificationEventsConsumer.cs`

Etki:
- Hangi notification'lar policy-driven, hangileri explicit request ile geliyor konusu tekrar bulaniklasabiliyor.

Onerilen sonraki adim:
Bu event resmi olarak desteklenecekse amacini dokumante et. Desteklenmeyecekse kaldir ve sade bir politika modeli birak.

## 3. ProjectService

### 3.1 Query tarafi hala owner-centric

Sorun:
Project membership modeli eklenmis olsa da proje listeleme sorgulari hala sadece `OwnerUserId` uzerinden calisiyor.

Neden acik:
- `src/services/projects/ProjectService.Infrastructure/Repositories/ProjectRepository.cs`
- `src/services/projects/ProjectService.Application/Features/Projects/Queries/GetProjectsByUser/GetProjectsByUserQueryHandler.cs`
- `src/services/projects/ProjectService.Application/Features/Projects/Queries/GetProjectsByUserPaged/GetProjectsByUserPagedQueryHandler.cs`

Etki:
- `Owner/Admin/Member` rolleri read-side'da birinci sinif hale gelmiyor.
- Membership modeli kismen write-only metadata'ya donusuyor.

Onerilen sonraki adim:
`GetProjectsByUser` ve paged varyantini membership bazli calistir. Owner'i de membership'in bir parcası olarak ayni sorgu modeli icinde ele al.

## 4. SprintService

### 4.1 `SprintIssue` hala projection/domain hibriti gibi duruyor

Sorun:
`SprintIssue`, backlog/sprint board/query ihtiyaclarini tasiyan projection benzeri bir yapi olmasina ragmen domain katmaninda tutuluyor.

Neden acik:
- `src/services/sprints/SprintService.Domain/Entities/SprintIssue.cs`
- `src/services/sprints/SprintService.Infrastructure/Persistence/SprintDbContext.cs`

Etki:
- Sprint bounded context icinde read-side ile domain siniri netlesmiyor.

Onerilen sonraki adim:
`SprintIssue` icin de projection/read-side ayrimi acik hale getir. Domain davranisi gerekmiyorsa domain entity olmaktan cikar.

### 4.2 `AddIssue` akisi projection gecikmesine karsi kirilgan

Sorun:
Yeni issue olusturulduktan hemen sonra sprint'e ekleme denenirse, `IssueCreatedEvent` projection'i henuz gelmemisse `SprintIssue not found` hatasi aliniyor.

Neden acik:
- `src/services/sprints/SprintService.Application/Features/Sprints/Commands/AddIssue/AddIssueCommandHandler.cs`
- `src/services/sprints/SprintService.Api/Events/Handlers/IssueCreatedEventHandler.cs`

Etki:
- Eventual consistency yarisi kullaniciya dogrudan hata olarak yansiyor.

Onerilen sonraki adim:
Issue lookup stratejisini dayanıklı hale getir. Gerekirse source-of-truth servisten validation yap veya projection hazir degilse retry/pending mantigi uygula.

### 4.3 Velocity gecmise sabitlenmiyor

Sorun:
Velocity hesaplamasi halen mevcut projection durumuna gore yapiliyor; tamamlanmis sprint icin immutable snapshot mantigi yok.

Neden acik:
- `src/services/sprints/SprintService.Application/Features/Sprints/Queries/GetSprintVelocity/GetSprintVelocityQueryHandler.cs`

Etki:
- Gecmis sprint metrikleri sonradan degisebilir.

Onerilen sonraki adim:
Sprint tamamlanirken velocity/summary snapshot'i kalici olarak yaz ve query'leri bu immutable veriden besle.

## 5. IdentityService

### 5.1 Shared symmetric JWT secret modeli devam ediyor

Sorun:
Servisler halen ayni `Jwt:Secret` tabanli symmetric token validation modelini kullaniyor.

Neden acik:
- `src/services/identity/IdentityService.Api/Program.cs`
- `src/services/issues/IssueService.Api/Program.cs`
- `src/services/projects/ProjectService.Api/Program.cs`
- `src/services/storage/StorageService.Api/Program.cs`
- `src/services/sprints/SprintService.Api/Program.cs`
- `src/services/notifications/NotificationService.Api/Program.cs`

Etki:
- Tek bir servis compromise olursa token forge yuzeyi buyuyor.

Onerilen sonraki adim:
Asymmetric signing veya merkezi token validation yaklasimina gec. En azindan secret fallback kullanimini kaldir.

### 5.2 Refresh endpoint eksik

Sorun:
Refresh token handler mevcut ama API controller'da refresh endpoint tanimli degil.

Neden acik:
- `src/services/identity/IdentityService.Application/Features/Auth/Commands/Refresh/RefreshTokenCommandHandler.cs`
- `src/services/identity/IdentityService.Api/Controllers/AuthController.cs`

Etki:
- Session akisi yari kalmis gorunuyor.
- Kod seviyesinde desteklenen bir capability, API yuzeyinde ulasilamaz durumda.

Onerilen sonraki adim:
Refresh endpoint'i ekle veya refresh akisini tamamen kaldir. Mevcut ara durumda kalmasin.

### 5.3 Logout gercek revoke yapmiyor

Sorun:
Logout su anda yalnizca cookie siliyor; refresh token veritabani tarafinda revoke edilmiyor.

Neden acik:
- `src/services/identity/IdentityService.Api/Controllers/AuthController.cs`

Etki:
- Ele gecirilmis veya halen aktif refresh token'lar logout sonrasinda da kullanilabilir.

Onerilen sonraki adim:
Logout aninda refresh token revoke et. Mümkünse token family/session bazli invalidation ekle.

### 5.4 Identity lifecycle eventleri ve event publishing boslugu

Sorun:
Identity tarafinda `UserCreated`, `UserDeactivated`, `UserRoleChanged` gibi lifecycle event akislari gorunmuyor.

Neden acik:
- `src/services/identity/IdentityService.Api/Program.cs`
- `src/services/identity/IdentityService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

Etki:
- Diger servisler stale auth state ile calisabilir.
- Kullanici lifecycle degisiklikleri dagitik sisteme guvenilir bicimde yayilmaz.

Onerilen sonraki adim:
Identity icin outbox tabanli lifecycle event seti tanimla. Kullanici durum/rol degisimleri ve kritik security olaylari yayinlansin.

## 6. StorageService

### 6.1 Parent entity ownership hala yok

Sorun:
StorageService bilincli olarak sadece blob + minimal metadata tutuyor, ancak bu durumda parent issue/project baglamina gore yetki kontrolu yapamiyor.

Neden acik:
- `src/services/storage/StorageService.Domain/Entities/StoredFile.cs`
- `src/services/storage/StorageService.Api/Controllers/StorageController.cs`

Etki:
- Dosya erisimi sadece uploader/admin semantigine dayaniyor.
- Parent entity authorization ile storage authorization birbirinden kopuk kaliyor.

Onerilen sonraki adim:
Ya bu siniri resmi olarak kabul edip attachment authorization'i baska serviste merkezilestir, ya da finalize aninda parent ownership referansi tanimla.

## 7. Dogrulama Altyapisi

### 7.1 Kok solution dosyasi gecerli degil

Sorun:
Kokteki `BitirmeProject.sln` dosyasi gecersiz gorunuyor. Bu nedenle tum repo icin standart `dotnet build` dogrulamasi yapilamiyor.

Neden acik:
- `BitirmeProject.sln`

Etki:
- Kapsamli build dogrulamasi kirik.
- Kod taramasi yapilsa bile son entegrasyon dogrulamasi zayif kaliyor.

Onerilen sonraki adim:
Gecerli solution header ve proje referanslari ile `.sln` dosyasini duzelt veya gercek build entrypoint'i resmi olarak belirle.

## Oncelik Sirasi

1. Issue attachment akisini server-side ve guvenilir hale getirmek
2. `IssueBoardItem` ve `SprintIssue` projection/domain ayrimini tamamlamak
3. NotificationService icindeki HTTP enrichment bagimliligini kaldirmak
4. Project query tarafini membership bazli hale getirmek
5. Identity refresh/revoke/lifecycle event bosluklarini kapatmak
6. Kapsamli build dogrulamasini tekrar calisir hale getirmek
