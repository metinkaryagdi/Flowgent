NotificationService — Mimari İnceleme Notları
Kritik Problemler

Notification policy ownership tutarsız olabilir

Bazı akışlarda karar upstream serviste veriliyor:

örn. NotificationRequestedEvent

bazı akışlarda NotificationService business event’ten kendi policy’sini çıkarıyor:

IssueAssigned

CommentAdded

MemberAdded

Bu, servis sınırını bulanıklaştırıyor.

HTTP enrichment ile distributed monolith kokusu var

CommentAddedEvent alınıp sonra IssueService’e HTTP çağrısı yapılıyorsa,

transient hata notification loss’a dönüşebilir.

Queue topology riski burada da tekrar ediyor

Shared queue naming varsa NotificationService de event’leri diğer servislerle yarışarak tüketiyor olabilir.

Outbox burada da broken olabilir

Publish state persist edilmiyor olabilir.

Reliable delivery iddiası zayıflar.

Inbox/idempotency atomic olmayabilir

Handler işini bitirip sonra ProcessedEvent kaydediyorsa crash senaryosunda duplicate delivery oluşabilir.

Delivery semantiği tutarsız olabilir

NotificationRequestedEvent yolunda SignalR/email side-effect var

ama diğer handler’lar sadece DB’ye notification yazıyor olabilir

Aynı servis içinde iki farklı delivery modeli oluşuyor.

Katmanlama Sorunları

ProcessedEvent domain concern değil

Domain’den çıkarılması gerekebilir.

Inbox bookkeeping state olarak düşünülmeli.

Delivery side-effect’leri event handler içinde persistence ile iç içe olabilir

SignalR/email orchestration ayrı katman değilse sorumluluklar karışıyor.

Shared abstraction seviyesinde domain event / integration event ayrımı yine bulanık olabilir

Bu artık sistem genelinde tekrar eden tema.

Bounded Context / Ownership Notları

NotificationService ayrı bounded context olabilir

in-app/email delivery ve user-facing notification record burada olabilir.

Ama servis şu an iki rolü aynı anda oynuyor olabilir

delivery engine

policy engine

Boundary temiz değil olabilir

Özellikle CommentAdded akışında recipient seçmek için başka servise geri dönülmesi bunu gösteriyor.

Issue/Project domain’i GUID düzeyinde değil, karar bağımlılığı düzeyinde sızıyor olabilir

Bu ciddi bounded context sinyali.

Notification Domain Model Notları

Notification aggregate var gibi görünüyor

Bu olumlu.

Ama domain modeli delivery açısından sığ olabilir

Unread/Read tüketim state’idir

gerçek delivery lifecycle değildir

Delivery lifecycle eksik olabilir

Queued

Sent

Failed

Delivered
gibi durumlar görünmüyor olabilir.

Channel modeli dar olabilir

InApp ve Email var olabilir

ama per-channel attempt modeli yoksa production-grade değil.

Event Tasarımı Notları

Tüketilen event seti heterojen olabilir

NotificationRequestedEvent command-kılığında event gibi

diğerleri gerçek business event olabilir

NotificationRequestedEvent contract’ı kötü kokuyor olabilir

NotificationId taşıyor ama handler bunu kullanmıyorsa sözleşme anlamsız hale gelir.

NotificationCreatedEvent / NotificationReadEvent publish ediliyor ama consumer görünmüyor olabilir

Bu gereksiz event gürültüsü olabilir.

Event payload’lar notification üretmek için yetersiz olabilir

Sonradan HTTP enrichment ihtiyacı doğuruyorsa contract zayıf demektir.

Correlation propagation yine eksik olabilir

Bu da ortak sistemsel tema.

Veri Tutarlılığı Notları

CreateNotification local transaction içinde outbox ile birlikte yazılıyor olabilir

Bu olumlu niyet.

Ama dedup yarışa açık olabilir

ExternalEventId için sadece normal index varsa concurrent duplicate notification üretilebilir.

Transient enrichment hatası kalıcı notification loss’a dönüşebilir

Özellikle event ACK edilip exception fırlatılmıyorsa.

DLQ / poison message handling eksik olabilir

Yine ortak sistemsel tema.

Delivery Model Notları

Gerçek email provider entegrasyonu olmayabilir

NoOpEmailSender varsa yön doğru ama uygulama yarım.

Retry modeli eksik olabilir

Gerçek delivery retry görünmüyor olabilir.

Per-channel delivery attempt modeli eksik olabilir

email başarısız / in-app başarılı gibi durumlar ayrı izlenemeyebilir.

SignalR hub claim extraction ile controller claim extraction farklı olabilir

Aynı token API’de çalışıp hub tarafında farklı davranabilir.

Delivery state ile consumption state karışıyor olabilir

unread/read = kullanıcı okuma durumu

sent/delivered/failed = sistem teslim durumu

bunlar ayrı olmalı.

Güvenlik / Ownership Notları

Public API ownership zayıf olabilir

Create body’deki UserId ile notification oluşturuyorsa

GetByUser route’taki userId ile başkasının listesini dönebiliyorsa

spoofing / yetkisiz erişim riski var.

Trusted identity source eksik olabilir

User ownership body/route üzerinden değil claims/context üzerinden gelmeli.

DDD Notları

Servis saf notification orchestration/delivery context’i gibi değil

yarı policy engine

yarı delivery adapter
gibi duruyor olabilir.

Business event -> notification policy -> delivery zinciri tek prensiple ilerlemiyor

Bu da domain netliğini bozuyor.

NotificationService için geçici hüküm

Ayrı bounded context olarak mantıklı

ama şu an policy ownership, event contract tasarımı ve delivery orchestration açısından tutarsız

özellikle HTTP enrichment, broken fan-out/outbox, non-atomic inbox ve zayıf delivery modeli ciddi

major refactor adayı

Şu ana kadar ortaklaşan sistemsel temalar artık iyice net

Identity + Issue + Project + Sprint + Notification birlikte bakınca tekrar eden pattern’ler:

Shared queue topology problemi

Outbox güvenilirlik sorunu

Inbox/idempotency atomicity sorunu

Projection/read-side ile domain model karışması

Lifecycle event propagation eksikliği

Actor/correlation trusted context eksikliği

Bounded context ownership kararlarının bazı yerlerde belirsiz olması

DLQ / poison message / ordering stratejisinin eksikliği

Bazı servislerde public API ownership güvenliği zayıf

Domain event / integration event ayrımı sistemsel olarak bulanık