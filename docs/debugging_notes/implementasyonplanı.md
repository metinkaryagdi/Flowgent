Tamam, şimdi bunu servis servis değil, workstream + sıra + risk yönetimi şeklinde kuralım.
Amaç şu olacak:

sistemi bir anda parçalamadan, çalışan yapıyı kontrollü şekilde production-grade mimariye yaklaştırmak.

Ben sana burada detaylı ve uygulanabilir bir düzeltme planı vereceğim. Bu plan:

neyi önce yapacağınızı

neden önce onu yapacağınızı

hangi servisleri etkileyeceğini

neyin bağımlılık olduğunu

hangi aşamada kod davranışının değişeceğini

hangi aşamada sadece altyapı temizliği yapılacağını

tek tek ayıracak.

Genel strateji

Bu projede en büyük hata şu olur:

Her serviste aynı anda refactor başlatmak.

Bunu yaparsan:

eventler kırılır

test edemezsin

bug’ın hangi değişiklikten çıktığını anlayamazsın

sistem birkaç hafta “yarı bozuk” kalır

O yüzden düzeltme planı şu mantıkla gitmeli:

Önce

altyapı güvenilirliği

Sonra

bounded context ve ownership kararları

Sonra

workflow/domain model zenginleştirme

En son

iyileştirme ve ölçeklenebilirlik

Büyük resimde 6 faz

Foundation / Messaging Reliability

Trust & Security Corrections

Ownership & Boundary Cleanup

Workflow / Domain Modeling Fixes

Attachment & Notification Lifecycle Stabilization

Production Hardening & Observability

FAZ 1 — Foundation / Messaging Reliability
Amaç

Önce sistemi “mesaj kaybetmeyen, yanlış tüketmeyen, tekrar üretmeyi azaltan” hale getirmek.

Bu faz bitmeden başka büyük refactor’lara girmeyin. Çünkü şu an çıkan bütün review’larda ortak kırmızı alanlar şunlardı:

shared queue topology

broken outbox

non-atomic inbox

DLQ eksikliği

ordering/correlation eksikliği

Bunlar çözülmeden domain refactor yapmak, bozuk zemine bina dikmek olur.

1.1 RabbitMQ topology’yi düzelt
Sorun

Şu an queue isimleri event adına göre ortaklaşıyorsa pub/sub yerine competing consumer oluşuyor.

Hedef

Her subscriber kendi queue’suna sahip olacak.

Yapılacaklar

Exchange merkezli bir yapı tanımlayın.

Her event tipi için routing key standardı belirleyin.

Her servis için subscriber-specific queue tanımlayın.

Önerilen naming

Exchange:

pm.events

Routing key örnekleri:

issue.created

issue.assigned

issue.status.changed

project.archived

sprint.completed

notification.requested

Queue örnekleri

notification.issue-events.queue

sprint.issue-events.queue

project.issue-events.queue

Etkilenen servisler

Issue

Project

Sprint

Notification

ortak RabbitMQ abstraction

Çıktı

Aynı event aynı anda birden fazla servise güvenli şekilde fan-out edilir.

Kabul kriteri

Bir IssueCreated event’i publish edildiğinde hem Notification hem Sprint hem Project kendi queue’sunda aynı olayı alabilmeli.

Servislerden biri kapalıyken diğeri event’i tüketmeye devam etmeli.

1.2 Outbox gerçekten çalışır hale getir
Sorun

Outbox var ama publish sonucu persist edilmiyor gibi görünüyor.

Hedef

“DB commit olduysa event eninde sonunda yayınlanır” garantisine yaklaşmak.

Yapılacaklar

Outbox tablosunda en az şu alanlar olmalı:

Id

EventType

Payload

OccurredOn

Status (Pending, Published, Failed)

RetryCount

LastError

PublishedOn

NextRetryAt

LockId veya claim alanı

ClaimedUntil

Publisher davranışı

pending kayıtları batch halinde al

claim et

publish et

başarılıysa Published

başarısızsa Failed + retry metadata

status değişimini kesin persist et

Etkilenen servisler

tüm event publish eden servisler

ortak outbox publisher

Kabul kriteri

Uygulama publish sırasında çökse bile event outbox’ta görünmeli

Tekrar ayağa kalkınca yayın devam etmeli

Multi-instance durumda aynı event iki kez publish edilme riski minimuma inmeli

1.3 Inbox / ProcessedEvent yapısını atomic hale getir
Sorun

Consumer önce iş mantığını çalıştırıp sonra processed-event yazıyorsa crash aralığında duplicate işleme olur.

Hedef

“Bir event işlendi mi?” kontrolü ile işleme sonucunu aynı güvenli sınırda yönetmek.

Yapılacaklar

ProcessedEvent / inbox state domain’den çıkarılacak

Infrastructure veya Messaging altında tutulacak

Consumer handler ile processed mark aynı transaction scope içinde ele alınacak

EventId + ConsumerName unique constraint düşünülecek

Etkilenen servisler

Issue

Project

Sprint

Notification

Kabul kriteri

Aynı event ikinci kez gelirse state bozulmamalı

Crash sonrası duplicate counter / duplicate notification üretilmemeli

1.4 DLQ ve poison message stratejisi ekle
Sorun

requeue: false var ama DLQ yoksa mesaj sessizce kaybolabilir.

Hedef

İşlenemeyen mesajlar gözlenebilir ve tekrar incelenebilir olsun.

Yapılacaklar

Her kritik queue için DLQ tanımla

dead-letter exchange kur

retry sayısı sonrası mesajı DLQ’ya at

DLQ monitoring/logging ekle

Kabul kriteri

Hatalı payload sistemden kaybolmamalı

Sonradan inceleme yapılabilmeli

1.5 CorrelationId / Actor / Trace standardı getir
Sorun

Bazı servislerde actor/correlation body’den geliyor.

Hedef

Tüm servislerde trusted context üzerinden izlenebilirlik.

Yapılacaklar

CorrelationId middleware standardı netleştir

publish edilen tüm eventlere correlation ekle

actor bilgisi token/claims/context’ten gelsin

request body’den actor alma tamamen kapatılsın

Kabul kriteri

Bir kullanıcı aksiyonu Seq/log üzerinden uçtan uca takip edilebilmeli

FAZ 2 — Trust & Security Corrections
Amaç

Sistemi güvenlik açısından “kolay kırılır” durumdan çıkarmak.

Bu faz Faz 1’den hemen sonra gelmeli çünkü:

kimlik

dosya erişimi

user spoofing

açık endpoint’ler

çok ciddi risk.

2.1 IdentityService endpoint security düzelt
Yapılacaklar

public auth endpointleri ile admin/user-management endpointlerini ayır

update/delete/create role gibi endpointlere policy bazlı auth koy

body’den gelen user identity kullanımını kaldır

normalized email/username/role stratejisi ekle

Ek işler

brute force/rate limit planı

lockout alanları

token revocation yaklaşımı

refresh token hashing

Kabul kriteri

anonim kullanıcı admin işi yapamasın

kullanıcı başkasının hesabını route/body manipülasyonuyla etkileyemesin

2.2 StorageService access control düzelt
Yapılacaklar

download/delete için owner veya parent-entity authorization getir

uploadedByUserId form/body’den alınmasın

claims/context’ten türet

file access sadece auth değil, ownership/policy bazlı olsun

Kabul kriteri

login olmuş rastgele kullanıcı başka dosyayı ID ile indiremeyecek

2.3 Notification ve diğer servislerde user spoofing temizliği
Yapılacaklar

route/body userId ile “başkasının verisini görme/oluşturma” akışlarını kapat

trusted identity source standardını tüm servislerde uygula

FAZ 3 — Ownership & Boundary Cleanup
Amaç

Sistemin en karışık kısmını temizlemek:
hangi gerçeğin sahibi hangi servis?

Bu faz en kritik tasarım fazı. Burada yanlış karar verirsen sonraki bütün kod yanlış yere kurulur.

3.1 Sprint assignment ownership kararını ver

Bu konu en büyük tasarım düğümlerinden biri.

Bugünkü sorun

SprintService SprintIssue.SprintId tutuyor

IssueService Issue.SprintId tutuyor

Bu aynı gerçeğin iki write modeli demek.

Karar vermeniz gereken seçenekler
Seçenek A — Sprint ownership SprintService’de

Bu durumda:

sprint’e ekleme/çıkarma kararının sahibi SprintService

IssueService içinde sprint bilgisi varsa yalnız projection/read-side olur

Issue aggregate write state’inden SprintId çıkarılabilir veya açıkça projection diye ayrılır

Artısı

Sprint bounded context daha net olur.

Eksisi

Issue detay ekranında sprint bilgisi projection ile gelir.

Seçenek B — Sprint membership owner IssueService

Bu durumda:

sprint ilişkisi issue’nun alanı olur

SprintService sadece lifecycle ve planlama tutar

SprintIssue read-side olur

Artısı

Issue merkezli kullanım kolay olabilir.

Eksisi

Sprint planning bounded context zayıflar.

Benim eğilimim

Bu projede Sprint assignment owner olarak SprintService daha mantıklı.
Çünkü sprint planning/sprint membership doğası gereği sprint bounded context’e daha yakın.

Ama bunu kesinleştirmeden kod taşımayın.

Karar sonrası yapılacak

karşı taraftaki duplicate write state projection’a indirilecek

event contract buna göre sadeleştirilecek

3.2 Project aggregate içindeki issue summary state’i çıkar
Sorun

IssueCount, OpenIssueCount vs Project domain state’i değil.

Hedef

Project aggregate saf kalsın, summary ayrı read model olsun.

Yapılacaklar

ProjectSummary veya ProjectMetricsProjection gibi ayrı read model tasarla

issue eventlerinden projection güncelle

Project write modelini sadeleştir

Kabul kriteri

Project aggregate sadece project lifecycle ve membership taşısın

3.3 Notification policy ownership kararını ver
Bugünkü sorun

Bazen upstream karar veriyor, bazen NotificationService.

İki seçenek var
Model 1 — NotificationService policy owner

tüm servisler sadece business event publish eder

NotificationService recipient/channel/content policy çıkarır

Model 2 — Upstream command model

servisler NotificationRequested benzeri açık komut yollar

NotificationService sadece delivery yapar

Benim önerim

Bu projede Model 1 daha temiz:

IssueCreated

IssueAssigned

CommentAdded

SprintCompleted
gibi business eventler gelsin,
NotificationService policy’yi kendi içinde belirlesin.

Neden?

Çünkü NotificationService o zaman gerçekten ayrı bounded context olur.

3.4 Storage ownership modelini netleştir
Sorun

Storage ne?

saf blob storage mı

attachment lifecycle owner mı

Benim önerim

Şu aşamada StorageService’i saf file storage + metadata service yapın.
Attachment lifecycle owner olarak davranmasın.

Ama:

attachment relation sözleşmesi net olsun

orphan cleanup ve compensation olsun

Sonuç

Storage binary ve minimal file metadata owner

IssueService kendi attachment relation bilgisinin owner’ı

aradaki ilişki event/saga/cleanup ile yönetilir

FAZ 4 — Workflow / Domain Modeling Fixes
Amaç

Şimdi artık altyapı ve sınırlar toparlandıktan sonra domain’i güçlendirmek.

4.1 Sprint workflow’u gerçek hale getir
Bugünkü eksikler

date range yok

capacity yok

overlap kuralı yok

close sonrası carry-over politikası yok

completed sprint immutable değil

Yapılacaklar

StartDate, EndDate, Goal, Capacity gibi alanlar düşün

active sprint uniqueness DB constraint ile de korunmalı

completed sprint immutable olacak

close sonrası incomplete issue politikası netleşecek:

backlog’a dön

sonraki sprint’e taşı

manual karar bekle

Kabul kriteri

sprint kapanınca historical metrics değişmemeli

4.2 Project membership modelini zenginleştir
Eksikler

owner/member/admin ayrımı zayıf

permission modeli yok

owner project member olarak görünmeyebilir

Yapılacaklar

ProjectMemberRole ekle

owner’ı member olarak da tut

transfer ownership use case’i tasarla

remove member / self-remove / owner remove kuralları ekle

4.3 Issue domain’i güçlendir
Yapılacaklar

invariant’ları entity ve domain service arasında net dağıt

domain event stratejisi oluştur

IssueBoardItem projection’a taşınacak

ProcessedEvent domain’den çıkacak

comment/attachment/audit aggregate sınırları yeniden değerlendirilecek

4.4 Notification delivery lifecycle oluştur
Bugünkü eksik

Unread/Read var ama delivery yok.

Yapılacaklar

Ayrı kavramlar tanımla:

Notification record

Delivery attempt

Channel status

Örnek status:

Queued

Sent

Failed

Delivered

Kabul kriteri

in-app başarılı, email başarısız gibi ayrık durumlar izlenebilmeli

4.5 Identity session lifecycle güçlendir
Yapılacaklar

refresh token hashlenmiş saklansın

revoke-all strategy

role/status değişince token invalidation planı

security stamp/session version düşün

FAZ 5 — Attachment & Notification Lifecycle Stabilization
Amaç

En kırılgan iki süreç:

dosya

bildirim

Bunlar çok side-effect içerdiği için ayrı fazı hak ediyor.

5.1 Upload → Attach akışını güvenli hale getir
Bugünkü durum

Client önce upload yapıyor, sonra issue attach. Arada transaction yok.

İki pratik çözüm yolu
Yol A — Temporary upload + finalize

dosya temp olarak yüklenir

issue attach başarılı olursa finalize edilir

finalize olmayan temp dosyalar cleanup job ile silinir

Yol B — Event choreography

file uploaded

issue attach event/command

başarısızsa compensation ile file silinir

MVP için önerim

Yol A daha basit ve uygulanabilir.

5.2 Orphan cleanup job ekle
Yapılacaklar

metadata var ama binary yok mu?

binary var ama relation yok mu?

stale temp upload var mı?

Bunları tarayan reconciliation job yazın.

5.3 Notification delivery’yi handler içinden çıkar
Bugünkü sorun

Event handler içinde hem DB hem SignalR/email side-effect olabilir.

Hedef

İki aşamalı model:

notification record oluştur

delivery worker bunu işler

Sonuç

retry yönetimi kolaylaşır

duplicate side-effect daha iyi kontrol edilir

delivery ayrı ölçeklenir

FAZ 6 — Production Hardening & Observability
Amaç

Sistem çalışsın değil, gözlenebilir ve işletilebilir olsun.

6.1 Seq + structured logging standardı

Her event/command log’unda:

correlation id

actor id

entity id

event id

consumer adı

olsun.

6.2 Health check ve background worker monitoring

outbox worker health

DLQ depth

failed delivery count

orphan cleanup sonuçları

retry yoğunluğu

6.3 Event schema/version stratejisi

Özellikle ortak contract’larda:

payload sürümleme

backward compatibility

hardcoded alanların temizlenmesi

6.4 Replay / rebuild stratejisi

Projection’lar bozulursa:

rebuild nasıl yapılacak?

replay hangi kaynaktan?

ordering nasıl korunacak?

Bu en azından tasarım seviyesinde netleşmeli.

Uygulama sırası — En güvenli yol

Şimdi bunu en pratik sıraya koyuyorum.

Aşama A — Altyapı sabitleme

RabbitMQ topology düzelt

Outbox düzelt

Inbox atomicity düzelt

DLQ ekle

Correlation/actor standardı getir

Aşama B — Güvenlik

Identity endpoint security

Storage file access authz

user spoofing temizliği

Aşama C — Sınır kararları

Sprint ownership kararı

Notification policy ownership kararı

Storage ownership kararı

Project summary read-side’a taşı

Aşama D — Domain zenginleştirme

Sprint workflow

Project membership/authorization

Issue domain cleanup

Notification delivery lifecycle

Identity session lifecycle

Aşama E — Side-effect stabilization

upload-finalize / compensation

orphan cleanup

delivery worker modeli

Aşama F — Sertleştirme

schema versioning

monitoring

replay/rebuild stratejisi

Servis bazlı değil, takım bazlı çalışma önerisi

Bunu böyle yönetmek daha verimli olur:

Workstream 1 — Messaging Platform

RabbitMQ

outbox

inbox

DLQ

correlation

Workstream 2 — Security & Trust

Identity

actor context

endpoint authz

storage authz

Workstream 3 — Domain Boundaries

Sprint/Issue ownership

Notification ownership

Project summary extraction

attachment ownership

Workstream 4 — Workflow Modeling

sprint

notification delivery

identity session

membership model

Workstream 5 — Cleanup & Operability

orphan cleanup

monitoring

health

replay

Bu planın kritik prensibi

Her faz sonunda sistem deploy edilebilir halde kalmalı.

Yani:

bir faz bitmeden diğerine geçmeyin

ownership kararı vermeden duplicate state kaldırmayın

outbox düzeltmeden event payload redesign yapmayın

authz düzeltmeden public endpoint açmayın

İlk sprintte ne yapılmalı?

Ben olsam ilk geliştirme sprintine şunları koyarım:

RabbitMQ topology fix

Outbox persistence fix

Inbox atomicity tasarımı

DLQ ekleme

Actor/correlation trusted context standardı

Çünkü bunlar tüm servisleri aynı anda iyileştirir.

İkinci sprintte

Identity authz cleanup

Storage file access authz

Notification policy ownership kararı

Sprint ownership kararı

Üçüncü sprintte

Project summary’yi projection’a taşı

Sprint workflow kuralları

Notification delivery lifecycle

Upload/finalize veya compensation modeli

En kritik 5 öncelik

Eğer çok dar zaman varsa, önce bunları yap:

Queue topology

Outbox persistence

Inbox atomicity

Actor/user spoofing temizliği

Sprint assignment ownership kararı

Bunlar çözülürse sistem bir anda çok daha sağlıklı hale gelir.