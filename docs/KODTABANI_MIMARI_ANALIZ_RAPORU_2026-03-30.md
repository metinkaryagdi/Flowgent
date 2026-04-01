# BitirmeProject - Kodtabani Mimari Analiz Raporu

Hazirlanma tarihi: 2026-03-30

Bu rapor, repository'nin mevcut yerel calisma agaci uzerinden hazirlandi. Yani degerlendirme sadece commit'lenmis tarihsel durumu degil, analiz anindaki local degisiklikleri de kapsar.

## 1. Yonetici Ozeti

BitirmeProject, .NET 9 uzerinde kurulu, mikroservis tabanli bir proje/issue yonetim platformu. Sistem yalnizca CRUD agirlikli bir web uygulamasi degil; dagitik mimari, CQRS, event-driven entegrasyon, outbox tabanli guvenilir mesajlasma, projection/read-model mantigi, background worker'lar ve gercek zamanli bildirim gibi daha olgun kurumsal tasarim kararlarini da iceriyor.

Kod tabaninin bugunku haliyle ana karakteri su:

- Backend cekirdegi: `net9.0`, ASP.NET Core, EF Core, PostgreSQL, MediatR, FluentValidation, AutoMapper
- Entegrasyon omurgasi: RabbitMQ + Transactional Outbox + idempotent consumer yaklasimi
- Erisim modeli: JWT + HttpOnly cookie + claims tabanli kimlik aktarimi
- UI tarafi: React 19 + TypeScript + Vite + Zustand + Axios + DnD Kit + SignalR
- Altyapi: Docker Compose ile tam sistem orkestrasyonu, Redis, Seq, RabbitMQ, servis bazli Postgres instance'lari

Genel olarak mimari tutarli, servis sinirlari anlamli ve katman ayrimi belirgin. Bununla birlikte tum servisler tamamen ayni olgunluk seviyesinde degil; ornegin StorageService daha yaln bir servis olarak tasarlanmis, NotificationService email kanalinda placeholder implementasyon kullaniyor, frontend tarafinda ise React Query paketi kurulu olsa da fiili veri akisi halen agirlikli olarak `useState/useEffect + Axios` ile yonetiliyor.

## 2. Cozumun Ust Seviye Haritasi

Repository ana bolumleri:

| Yol | Rol |
|---|---|
| `src/services/*` | Mikroservisler ve BFF |
| `src/gateway/ApiGateway` | YARP tabanli API gateway |
| `src/frontend/web` | React SPA frontend |
| `src/shared/*` | Ortak abstractions, common utility ve event contract kutuphaneleri |
| `tests/*` | Unit ve integration test projeleri |
| `docs/*` | Proje notlari, planlar ve sunum belgeleri |

Servis envanteri:

| Bilesen | Gorev |
|---|---|
| IdentityService | Kimlik dogrulama, rol ve kullanici yonetimi |
| ProjectService | Proje ve proje uyeligi yonetimi |
| IssueService | Issue yasam dongusu, yorum, attachment, board projection |
| SprintService | Sprint planlama, backlog ve velocity/summary mantigi |
| NotificationService | Bildirim uretimi, teslimi, SignalR ile iletim |
| StorageService | Dosya metadata + filesystem blob yonetimi |
| Bff.Api | Frontend icin veri toplulastirma ve UI flag endpoint'leri |
| ApiGateway | Tek giris noktasi, reverse proxy ve JWT dogrulama |

## 3. Runtime Topolojisi

Sistemin calisma zamani topolojisi ozetle su:

```text
React Frontend
    |
    v
API Gateway (YARP)
    |
    +--> IdentityService
    +--> ProjectService
    +--> IssueService
    +--> SprintService
    +--> NotificationService
    +--> StorageService
    +--> BFF

Servisler arasi async iletisim:
    Tum event yayinlari -> RabbitMQ topic exchange: bitirme_events

Kalici veri:
    Her ana servis -> ayri PostgreSQL veritabani
    Secili read/query akislari -> Redis cache
    Loglar -> Seq
    Dosyalar -> StorageService altinda local filesystem root
```

Docker Compose tarafinda ayri container'lar tanimli:

- RabbitMQ
- Redis
- Seq
- 6 ayri PostgreSQL container'i
- 6 domain servisi
- BFF
- Gateway
- Frontend

Bu, "database per service" kararinin altyapi seviyesinde de gercekten uygulandigini gosteriyor.

## 4. Kullanilan Teknolojiler

### Backend

| Teknoloji | Durum |
|---|---|
| .NET SDK 9.0.101 | `global.json` ile sabitlenmis |
| ASP.NET Core 9 | Tum API'lerin temel runtime'i |
| EF Core 9 + Npgsql | Code-first migration ve persistence katmani |
| MediatR 14 | Command/query handler orkestrasyonu |
| FluentValidation 12 | Request dogrulama |
| AutoMapper 16 | DTO mapleme |
| Serilog + Seq | Yapilandirilmis loglama |
| JWT Bearer Auth | Gateway + servislerde savunmali dogrulama |

### Mesajlasma ve Dagitik Sistem Bilesenleri

| Teknoloji | Durum |
|---|---|
| RabbitMQ.Client | Ortak event bus altyapisi |
| Transactional Outbox | Tum event-yayinlayan servislerde var |
| ProcessedEvents tablolari | Event idempotency icin kullaniliyor |
| DLQ topolojisi | Consumer tarafinda explicit tanimlanmis |
| Correlation ID | Middleware + event metadata seviyesinde tasiniyor |

### Frontend

| Teknoloji | Durum |
|---|---|
| React 19 | SPA yapisi |
| TypeScript 5.9 | Tip guvenligi |
| Vite 7 | Build/dev altyapisi |
| React Router 7 | Route yonetimi |
| Zustand | Auth, theme ve toast store'lari |
| Axios | HTTP client |
| SignalR JS client | Gercek zamanli bildirim akis denemesi |
| DnD Kit | Board drag-and-drop |
| Playwright | E2E test |
| `@tanstack/react-query` | Paket yuklu, ancak kaynak kodda aktif kullanim gorulmedi |

### Test Altyapisi

| Teknoloji | Durum |
|---|---|
| xUnit | Ana test framework'u |
| FluentAssertions | Assertion standardi |
| NSubstitute | Mock/stub katmani |
| Testcontainers.PostgreSql | Integration test veritabani izolasyonu |
| ASP.NET Core `WebApplicationFactory` | In-memory uygulama host'u |

## 5. Servis Bazli Mimari Analiz

## 5.1 IdentityService

Temel sorumluluklari:

- Register, login, refresh, logout
- Kullanici ve rol yonetimi
- Refresh token saklama
- Security stamp tabanli token gecersizlestirme
- Basarisiz giris sayaci ve lockout

Dikkat ceken teknik noktalar:

- `AuthController` access ve refresh token'i JSON body'de degil, HttpOnly cookie olarak set ediyor.
- JWT icine `security_stamp` claim'i ekleniyor; request dogrulamasinda DB'deki kullanici ile karsilastiriliyor.
- Kullanici aktif degilse veya security stamp uyusmuyorsa token aninda gecersiz sayiliyor.
- Startup'ta migration calisiyor ve varsayilan roller seed ediliyor.
- `UserCreatedEvent` outbox'a yaziliyor.

Yapisal not:

- IdentityService diger servislerle ayni paylasilan `AggregateRoot<TId>` soyutlamasini kullanmiyor; kendi `BaseEntity` yapisi ve konfigurasyonlari var.
- Yani bounded context mantigi var, fakat servisler arasi domain tabani tamamen uniform degil.

## 5.2 ProjectService

Temel sorumluluklari:

- Proje olusturma/guncelleme/silme
- Proje uyeligi ve uye rolleri
- Proje ozeti/sayac read model'i

Teknik karakter:

- `Project`, `ProjectMember`, `ProjectSummary` birlikte calisiyor.
- `ProjectMember` icin composite key kullaniliyor.
- `Project.Key` unique index ile korunuyor.
- Query tarafinda kullanici projeleri Redis ile 2 dakikalik cache'leniyor.
- Servis, Issue event'lerini tuketerek sayisal summary/projection guncelliyor.
- Outbox ile `ProjectCreatedEvent`, `ProjectUpdatedEvent`, `MemberAddedEvent` gibi event'ler uretiliyor.

Mimari yorumu:

- ProjectService, hem yazma modeli hem de projection/summary modeli tasiyan hibrit bir bounded context.
- Bu servis, IssueService'den gelen event'lerle kendi gorunumunu guncelliyor; dolayisiyla eventual consistency burada tasarimin merkezinde.

## 5.3 IssueService

Temel sorumluluklari:

- Issue CRUD
- Atama
- Durum gecisleri
- Yorumlar
- Attachment metadata linkleme
- Audit trail
- Kanban board projection

Teknik karakter:

- `Issue.Version` concurrency token olarak model'lenmis.
- Controller, `If-Match` veya `X-Expected-Version` header'larindan beklenen versiyonu okuyabiliyor.
- Guncelleme handler'lari version kontrolu yapiyor; boylece optimistic locking uygulanmis oluyor.
- `IssueBoardItem` read model'i ayri tutuluyor ve board query'leri Redis ile cache'leniyor.
- `CreateIssue`, `AssignIssue`, `ChangeIssueStatus` gibi write operasyonlari cache invalidation yapiyor.
- Attachment akisi dogrudan binary tasimiyor; `fileId` uzerinden StorageService metadata dogrulamasi yapiyor.
- Ayni dosyanin ayni issue'ya ikinci kez baglanmasi unique index ile de korunuyor.

Mimari yorumu:

- Bu servis, domain davranisi acisindan en zengin bounded context'lerden biri.
- Hem write-side aggregate mantigi hem read-model/projection mantigi acik sekilde gorunuyor.
- Asenkron entegrasyon ile sprint baglantisini, senkron HTTP ile de storage dogrulamasini birlestiren hibrit bir servis.

## 5.4 SprintService

Temel sorumluluklari:

- Sprint olusturma
- Sprint baslatma / tamamlama
- Backlog ve sprint issue iliskisi
- Sprint summary/velocity verileri

Teknik karakter:

- `Sprint.Status` state machine mantigina sahip.
- Completed sprint immutable kabul ediliyor.
- DB tarafinda filtreli unique index ile proje basina tek aktif sprint korunuyor.
- `SprintIssue` bir projection/read model gibi kullaniliyor.
- `AddIssueCommandHandler`, event gecikmesi halinde `IssueService`'e gidip issue metadata'sini cekerek projection'i on-the-fly olusturabiliyor.

Mimari yorumu:

- SprintService, eventual consistency kaynakli race condition'lari kabul ediyor ve belirli noktalarda senkron fallback kullanarak kullanici deneyimini koruyor.
- Bu, pratik bir mikroservis tavri; tam saf asenkronluk yerine kontrollu melezlestirme var.

## 5.5 NotificationService

Temel sorumluluklari:

- Bildirim olusturma
- Bildirimleri kullaniciya listeleme
- Okundu bilgisi
- In-app ve email kanallari
- Gercek zamanli iletim

Teknik karakter:

- Event consumer tarafi `IssueAssignedEvent`, `IssueStatusChangedEvent`, `CommentAddedEvent`, `MemberAddedEvent` event'lerini dinliyor.
- `ProcessedEvents` tablosu ile duplicate event isleme engelleniyor.
- `NotificationDeliveryWorker`, kuyruktaki uygun bildirimleri periyodik isleyip channel'a gore teslim etmeye calisiyor.
- In-app kanalinda SignalR group (`user-{userId}`) uzerinden push yapiliyor.
- Email kanalinda `NoOpEmailSender` kayitli; yani email davranisi uretime hazir degil, placeholder seviyesinde.
- `NotificationReadEvent` outbox'a yaziliyor.

Mimari yorumu:

- NotificationService, domain event tuketimi ile runtime delivery islevini ayni serviste topluyor.
- In-app notification akisi gercek, email akisi ise su an altyapi iskeleti seviyesinde.

## 5.6 StorageService

Temel sorumluluklari:

- Dosya upload
- Temporary -> finalized durum gecisi
- Metadata saklama
- File content stream etme
- Orphan ve expired file temizligi

Teknik karakter:

- Veritabani yalnizca metadata tutuyor; binary icerik local filesystem root altinda saklaniyor.
- Path traversal'a karsi `ResolvePath` icinde root disina cikis kontrolu yapiliyor.
- `StoredFile.Status` ile gecici ve kalici dosya ayrimi korunuyor.
- Cleanup worker hem suresi gecmis temporary metadata kayitlarini hem de orphan temp binary'lerini temizliyor.
- Bu servis RabbitMQ/outbox altyapisina bagli degil; senkron servis olarak tasarlanmis.

Mimari yorumu:

- StorageService, bilincli olarak "minimum domain, maksimum izolasyon" prensibiyle tutulmus.
- Parent entity authorization'i burada degil, ust serviste yapiliyor; bu da boundary kararinin bilerek verildigini gosteriyor.

## 5.7 BFF

Temel sorumluluklari:

- Frontend'in ihtiyac duydugu bilesik endpoint'leri sunmak
- UI flag hesaplamak
- Board ekrani icin Project + Issue + Workflow verilerini tek response'ta toplamak
- Active sprint ve notification listeleme gibi UI odakli sorgulari basitlestirmek

Teknik karakter:

- `HttpClientFactory` + Polly retry politikasi kullaniyor.
- BFF icinde yazma mantigi yok; daha cok composite query facade gibi davraniyor.
- Frontend'in role-based capability modelini `flags` endpoint'i ile netlestiriyor.

## 5.8 ApiGateway

Temel sorumluluklari:

- Tek giris noktasi olmak
- YARP ile path bazli reverse proxy yapmak
- JWT dogrulama
- CORS ve correlation middleware

Teknik karakter:

- `/api/v1/*` rotalari cluster bazli servis adreslerine map edilmis.
- Cookie'den `accessToken` okuyup auth pipeline'ina veriyor.
- Gateway seviyesinde auth olmasina ragmen domain servisler de ayrica auth yapiyor; bu defense-in-depth yaklasimi.

## 5.9 Frontend

Temel sorumluluklari:

- Auth akislari
- Project, board, sprint, notification ve admin ekranlari
- Drag-and-drop board deneyimi
- Cookie tabanli auth ile SPA kullanimi

Teknik karakter:

- `BrowserRouter` tabanli klasik SPA mimarisi var.
- Zustand store'lari auth, tema ve toast icin kullaniliyor.
- Axios client `withCredentials: true` ile calisiyor.
- 401 yanitlarinda refresh endpoint'i tetiklenip istek yeniden deneniyor.
- Notification sayfasi SignalR hook'u ile tekrar fetch mantigi kullaniyor.
- Board sayfasi oldukca feature-rich; DnD, filtreleme, sprint atama, issue paneli ve modal akislari tek ekranda toplanmis.

Onemli gozlem:

- `@tanstack/react-query` paketi kurulu olsa da aktif `QueryClient`, `useQuery`, `useMutation` kullanimina rastlanmadi.
- Frontend veri yonetimi bugun itibariyla daha cok "custom API wrappers + local component state + useEffect" modelinde.

## 6. Katmanlama ve Bagimlilik Yonleri

Cogu domain servis asagidaki yapida:

```text
Api
  -> controller, middleware, hosted service consumer
Application
  -> command/query, handler, validator, DTO, abstraction
Domain
  -> entity, enum, business rule
Infrastructure
  -> EF Core, repository, client, DI, persistence
```

Bu katmanlama ozellikle Project, Issue, Sprint ve Notification servislerinde net.

Istisnalar:

- IdentityService ayni genel yone sahip olsa da kendi domain base class yapisini kullaniyor.
- StorageService daha kucuk bir servis oldugu icin event-driven cross-cutting pattern'lerin bir kismini tasimiyor.
- Gateway ve BFF bu dort katmanli yapinin disinda, daha cok application edge bilesenleri gibi konumlanmis.

## 7. Kullanilan Temel Pattern ve Tasarim Kararlari

## 7.1 Clean Architecture / Layered Service Design

Katman sinirlari acik. Controller'lar handler cagiriyor, handler'lar repository abstraction kullaniyor, infrastructure somut repository sagliyor. Bu, test yazimini kolaylastiriyor ve servis ici bagimlilik yonunu temiz tutuyor.

## 7.2 CQRS + MediatR Pipeline

Command ve query nesneleri ayri. Validator'lar MediatR pipeline icinde calisiyor. Bu tasarim:

- HTTP katmanini zayif tutuyor
- Validation logic'i merkezilestiriyor
- Handler bazli unit test yazimini kolaylastiriyor

## 7.3 Transactional Outbox

Event yayinlayan servislerde is verisi ile event kaydi ayni transaction kapsamina alinmis. Sonrasinda ortak `OutboxPublisherService` bu kayitlari batch claim edip RabbitMQ'ya publish ediyor.

Bu, dagitik sistemlerde en kritik saglamlik kararlarindan biri.

## 7.4 Idempotent Consumer / Inbox Benzeri Yaklasim

Consumer servisler `ProcessedEvents` tablosu tutuyor. Event tekrar gelirse handler yeniden calistirilmiyor. Bu sayede RabbitMQ'nun at-least-once semantigi uygulama seviyesinde effectively-once davranisa yaklastiriliyor.

## 7.5 Eventual Consistency + Projection

Asagidaki yapilar event veya write model uzerinden turetilmis projection/read model karakteri tasiyor:

- `ProjectSummary`
- `IssueBoardItem`
- `SprintIssue`
- `SprintSummary`

Bu, ozellikle board, sayac ve velocity ekranlarini verimli hale getiriyor.

## 7.6 Optimistic Concurrency

Issue guncellemelerinde version tabanli cakisma kontrolu var. UI ayni issue uzerinde eszamanli degisiklik yaparsa sistem sessizce overwrite etmiyor; explicit conflict mantigi kurulmus.

## 7.7 Claims-Based Trust Boundary

Birden fazla yerde su ilke korunuyor:

- Kullanici kimligi request body'den alinmaz
- Claims veya cookie tabanli dogrulanmis token esas alinir

Bu karar ozellikle auth, storage ve notification endpoint'lerinde goruluyor.

## 7.8 Background Worker Pattern

Worker kullanan ana akislar:

- Outbox publishing
- Notification delivery
- Storage orphan cleanup
- RabbitMQ consumer hosted service'leri

Bu da uygulamanin yalnizca request/response degil, surekli calisan asynchronous process'ler barindirdigini gosteriyor.

## 8. Veri Akisi ve Entegrasyon Tarzi

Sistem tamamen event-driven degil; kontrollu hibrit bir model var.

### Asenkron akislara ornek

- Issue olusturma -> Project/Sprint tarafinda projection guncellenmesi
- Issue status degisimi -> Notification ve summary guncellemeleri
- Member added -> Notification uretimi

### Senkron HTTP akislara ornek

- BFF -> domain servisleri toplulastirarak cagirma
- SprintService -> IssueService metadata fallback
- IssueService -> StorageService metadata dogrulamasi

Bu secim, "her seyi event ile coz" dogmatizmi yerine, kullanici akislarini bozmadan dagitik sistem karmasini yonetmeye calisan pragmatik bir mimariyi isaret ediyor.

## 9. Guvenlik ve Gozlemlenebilirlik

Guvenlik tarafinda:

- JWT validation gateway'de ve servislerde mevcut
- HttpOnly cookie modeli kullaniliyor
- Refresh token body'de tasinmiyor
- Security stamp ile token invalidation uygulanmis
- Claims tabanli user extraction yaygin

Gozlemlenebilirlik tarafinda:

- Serilog + Seq standardi tum servislerde var
- Correlation middleware request'e `X-Correlation-Id` atiyor ve response'a geri yaziyor
- Event metadata tarafinda `EventId`, `CorrelationId`, `EventVersion` bulunuyor
- Worker sagligi icin health check'ler tanimli

Bu iki alan, kod tabaninin "sadece calissin" seviyesinin otesinde isletimsel olgunluga da yatirim yaptigini gosteriyor.

## 10. Test Mimarisi

Test envanteri:

- 8 unit test projesi
- 4 integration test projesi
- 1 Playwright E2E spec dosyasi

Unit test kapsami servislerin buyuk bolumunde genis:

- controller testleri
- handler testleri
- validator testleri
- domain davranis testleri
- consumer testleri
- middleware testleri

Integration test kapsami olan servisler:

- IdentityService
- ProjectService
- IssueService
- SprintService

Bu testler `WebApplicationFactory + Testcontainers.PostgreSql` kombinasyonu ile calisiyor.

Gozlem:

- NotificationService ve StorageService icin unit test var, fakat ayni duzeyde integration test gorunmuyor.
- Frontend tarafinda Playwright var, ama tek bir ana senaryo dosyasi gozleniyor; yani E2E temel akislari dogruluyor ancak kapsam henuz sinirli.

## 11. Kod Temelli Ozel Gozlemler

Asagidaki maddeler, dogrudan bugunku kaynak koddan cikan ve mimari resmi anlamak icin onemli olan notlar:

1. Frontend veri yonetimi beklenenden daha manuel.
   `@tanstack/react-query` bagimliligi kurulu olsa da pratikte veri yukleme ve refresh akislarinin cogu `useEffect`, local state ve custom API wrapper'larla yuruyor.

2. Event compensation altyapisi isim olarak mevcut ama aktif degil.
   `SagaCompensationRequestedEvent` contract'i var; buna karsilik aktif bir saga orkestratoru veya compensation consumer akisi gorunmuyor.

3. Identity bounded context'i ortak domain tabanindan kismen ayrisiyor.
   Diger servisler paylasilan `Shared.Abstractions.Domain` hattina daha yakin iken Identity kendi base entity modelini korumus.

4. StorageService bilerek event omurgasinin disinda tutulmus.
   Bu, servisler arasi uniformlugu biraz azaltiyor ama storage'i daha yaln ve operasyonel olarak sade yapiyor.

5. Email teslimati placeholder seviyesinde.
   Notification tarafinda `IEmailSender` icin `NoOpEmailSender` kullaniliyor.

6. Gateway config'i REST route'lara odakli.
   Mevcut YARP route tablosunda `/api/v1/*` yollar acikca var; Notification hub icin ayri bir `/hubs/notifications` proxy rotasi kodda gorunmuyor.

7. Sistem bugun itibariyla dirty working tree uzerinden okunuyor.
   Yani mimari rapor, repository'nin yalnizca commit'lenmis "stable" halini degil, su anki gelistirme durumunu da yansitiyor.

## 12. Genel Degerlendirme

BitirmeProject'in kod tabani, ogrenci projesi olceklerini asan, kurumsal mimari kavramlari pratikte uygulamaya calisan bir yapiya sahip. En guclu yonleri:

- servis sinirlarinin net olmasi
- outbox/idempotency gibi dagitik sistem kararlarinin gercekten kodlanmis olmasi
- auth ve claims boundary'lerinin ciddiye alinmasi
- background worker ve health check kullanimlari
- unit test temelinin guclu kurulmasi

Olgunlastirma acisindan en mantikli sonraki odak alanlari:

- frontend veri katmanini standartlastirmak
- Notification ve Storage icin integration/E2E kapsamini genisletmek
- SignalR/gateway entegrasyonunu tek giris noktasi acisindan netlestirmek
- event compensation/saga altyapisini ya aktiflestirmek ya da sadelestirmek
- servisler arasi mimari convention'lari daha uniform hale getirmek

Sonuc olarak bu repository, "mikroservis gorunumlu monolitik CRUD" seviyesinde degil; dagitik sistem problemlerine bilincli cevaplar ureten, ama halen bazi alanlarda standardizasyon ve uretim sertlestirmesi gerektiren, teknik olarak ciddi bir proje durumunda.
