ProjectService — Mimari İnceleme Notları
Kritik Problemler

Project aggregate içine issue summary state gömülmüş olabilir

IssueCount, OpenIssueCount, InProgressIssueCount, DoneIssueCount gibi alanlar Project domain state’i değil, projection/read model adayı.

Bu durum bounded context’i kirletiyor.

ProjectService gerçek source of truth gibi davranmıyor olabilir

IssueService ve SprintService ProjectId’yi doğrulamadan kabul ediyorsa:

var olmayan project’e issue açılabilir

archive edilmiş project’e sprint açılabilir

Queue topology riski burada da tekrar ediyor

Queue adı event tipine göre ortaklaşıyorsa fan-out bozulur.

Bu konu artık sistemsel ortak problem adayı.

Outbox burada da broken olabilir

Publish sonucu persist edilmiyor olabilir.

Multi-instance duplicate publish riski olabilir.

Inbox/idempotency atomic değil olabilir

Issue event işlenip summary güncellendikten sonra processed mark atılıyorsa crash senaryosunda çift sayaç oluşabilir.

Katmanlama Sorunları

ProcessedEvent yine domain concern değil

Infrastructure / inbox state olarak düşünülmeli.

Issue event handler’ları application katmanını bypass ediyor olabilir

API/consumer seviyesinden doğrudan repository + unit of work ile write model mutate ediliyorsa katman sınırı zayıf.

Read-side summary state aggregate içine gömülmüş

Bu, “projection ayrı dursun” yerine “projection’ı aggregate’e yapıştıralım” yaklaşımı gibi duruyor.

Domain event / integration event ayrımı shared abstraction seviyesinde bulanık olabilir

Bu da IssueService’deki gözlemle aynı tema.

Bounded Context / Ownership Soruları

Project capability kavramsal olarak doğru

Project lifecycle + project membership burada olabilir.

Ama Project context temiz sahiplenilmiyor olabilir

Membership modeli eksik

issue summary state sızmış

lifecycle propagation eksik

User tarafında sadece GUID tutulması iyi

Profile data kopyalanmıyor olması olumlu.

Ama user deactivate/delete senaryoları için reconciliation görünmüyor olabilir.

Project lifecycle ownership eksik olabilir

Archive/delete akışlarının diğer servislere yansıması zayıf.

Membership / Authorization Notları

Membership modeli eksik olabilir

Owner/admin/member ayrımı görünmüyor olabilir.

Permission modeli yok olabilir.

Owner ile member semantiği kopuk olabilir

Project oluşturulurken owner member olarak da eklenmiyorsa model tutarsız.

Query tarafı owner-centric olabilir

Üyelik bazlı erişim yerine sadece owner bazlı proje listesi dönüyorsa membership modeli write-only metadata’ya dönüşür.

Project-level authorization eksik olabilir

Sadece authenticated olmak yeterliyse herhangi bir kullanıcı herhangi bir project’i mutate edebilir.

Actor bilgisi trusted context’ten gelmiyor olabilir

route/body üzerinden gelen OwnerUserId, AddedByUserId, UpdatedByUserId domain audit güvenilirliğini bozar.

Event Tasarımı Notları

Lifecycle event seti eksik olabilir

ProjectCreated var gibi

ama ProjectArchived, ProjectDeleted, MemberRemoved gibi olaylar eksik olabilir

Delete adı ile davranış uyumsuz olabilir

“delete” endpoint’i gerçekte archive yapıyorsa semantik sorun var.

Bazı eventler redundant olabilir

ProjectUpdatedEvent ve ProjectSettingsUpdatedEvent aynı payload ile gidiyorsa ayrım anlamsız olabilir.

ProjectService issue eventlerini consume edip write aggregate güncelliyor olabilir

Bu çoğu durumda projection/read-side işidir, aggregate consistency işi değil.

Ordering/version guard eksik olabilir

Replay/reorder altında summary sayaçları bozulabilir.

IssueAssignedEvent consumer business value üretmiyor olabilir

Sadece log atıyorsa gereksiz runtime coupling olabilir.

Veri Tutarlılığı Notları

Local transaction niyeti doğru

Create/update/add-member akışlarında aggregate + outbox aynı commit’te yazılıyor olabilir.

Ama güvenilir publish hâlâ zayıf

Outbox persistence eksikliği sistemsel tekrar eden tema.

ProjectMembers ile Projects arasında FK eksik olabilir

Orphan row riski doğar.

User/project lifecycle değişimlerinde orphan data riski var

Özellikle user silinirse/deactivate olursa member kayıtları ne olacak sorusu açık.

Production readiness zayıf olabilir

DLQ yok

poison message handling eksik

ordering strategy görünmüyor

correlation propagation zayıf olabilir

DDD Notları

Project aggregate root seçimi doğru görünüyor

Ama root’un içine external issue stream’den türetilen sayaçlar koymak domain’i kirletiyor.

ProjectMember davranışsız data bag olabilir

Rol, çıkarma, owner transfer, self-removal, duplicate semantics gibi kurallar domain davranışı olarak görünmüyor olabilir.

Aggregate büyük değil, eksik

Sorun “şişmiş aggregate” değil

“zayıf ve yarım aggregate” olabilir

Ownership transfer / archive guard / concurrency control eksik olabilir

Bu da domain maturity eksikliğine işaret eder.

Read Model / Projection Notları

Gerçek CQRS görünmüyor olabilir

Query side doğrudan write model tablosunu okuyorsa projection ayrımı zayıf.

Project summary ayrı read model olmalı olabilir

Özellikle issue count/state breakdown bilgileri için.

Projection update logic dağınık olabilir

Merkezi projection pipeline yerine handler’lara dağılmış manuel sync yaklaşımı var gibi.

Replay / rebuild / drift correction zorlaşır

Bu mimari ileride bakım yükü çıkarır.

Saga / Lifecycle Notları

Project create için saga şart görünmüyor

Bu not olumlu.

Project archive/delete için lifecycle choreography veya saga gerekebilir

Aksi halde IssueService, SprintService, NotificationService project’in öldüğünü öğrenmeyebilir.

Membership removal / owner transfer de event gerektirebilir

Özellikle başka servisler access/read-model cache tutuyorsa.

ProjectService için geçici hüküm

Capability sınırı teoride doğru

Ama implementasyon seviyesinde Project bounded context temiz sahiplenilmiyor

özellikle projection state’in aggregate’e gömülmesi, membership modelinin eksikliği, lifecycle event setinin yarım olması ve EDA reliability eksikleri ciddi

major refactor adayı

Şu ana kadar ortaklaşan sistemsel temalar

IssueService + IdentityService + ProjectService notlarını birlikte okuyunca tekrar eden pattern’ler daha netleşti:

Queue topology sistemsel risk

Outbox güvenilirliği sistemsel risk

Inbox/idempotency atomicity sistemsel risk

Katman sınırları bazı servislerde bulanık

Lifecycle event propagation eksik

Authorization/actor context güvenilirliği zayıf

Projection ile domain state karışıyor