SprintService — Mimari İnceleme Notları
Kritik Problemler

Sprint assignment ownership iki servise bölünmüş olabilir

SprintService tarafında SprintIssue.SprintId

IssueService tarafında Issue.SprintId

Aynı gerçeğin iki write modelde tutulması split authority riski oluşturuyor.

Queue topology riski burada da tekrar ediyor

Shared queue naming varsa fan-out yine bozuluyor.

Bu artık neredeyse kesin sistemsel ortak problem gibi duruyor.

Outbox burada da broken olabilir

Publish sonucu persist edilmiyor olabilir.

Row-claim / lease yoksa duplicate publish riski var.

Inbox/idempotency atomic değil olabilir

Handler state save ediyor, sonra ProcessedEvent ekleniyorsa crash aralığında duplicate işleme oluşabilir.

Completed sprint mutable kalıyor olabilir

Kapanmış sprintten issue çıkarılabiliyorsa

velocity sonradan değişebiliyorsa

historical doğruluk bozulur.

Project lifecycle entegrasyonu yok olabilir

Archive/deleted project üzerinde sprint/backlog state yaşamaya devam edebilir.

Katmanlama Sorunları

ProcessedEvent domain’de durmamalı

Inbox state / infrastructure concern.

SprintIssue domain entity değil, projection/read-side adayı olabilir

Issue context’ten türeyen backlog/projection mirror gibi kullanılıyor olabilir.

API katmanı event consumer’ları application layer’ı bypass ediyor olabilir

Repository + unit of work ile doğrudan write-side mutate ediliyorsa katman sınırı zayıf.

Shared abstraction seviyesinde domain event / integration event ayrımı yine bulanık olabilir

Bu tema artık servisler arasında tekrar ediyor.

Bounded Context / Ownership Soruları

SprintService capability olarak ayrı servis olabilir

Sprint lifecycle ve sprint planning burada olabilir.

Ama şu anki haliyle sprint management tam modellenmemiş olabilir

scheduling yok

capacity yok

commitment yok

overlap modeli yok

carry-over politikası yok

Sprint bounded context temiz değil olabilir

Issue context şu alanlarla sızıyor olabilir:

Title

IssueType

Priority

Status

CreatedByUserId

Project boundary çok zayıf

Sadece ProjectId taşınıyor olabilir.

Project lifecycle doğrulaması / event entegrasyonu görünmüyor olabilir.

Event Tasarımı Notları

SprintCreatedEvent eksik olabilir

Start ve complete publish ediliyor ama create edilmiyorsa diğer servisler sprint varlığını asenkron öğrenemez.

IssueAddedToSprint / IssueRemovedFromSprint business anlam taşıyor ama fiilen uzaktan state mutation emri gibi davranıyor olabilir

Bu önemli tasarım sinyali.

Issue event contract’ları eksik olabilir

Örn. IssueCreatedEvent status taşımıyorsa SprintService status’u hardcode ediyor olabilir.

Ordering / version metadata eksik olabilir

Out-of-order event’lerde backlog/projection bozulabilir.

Warning verip ack’leme riski olabilir

Out-of-order event geldiğinde sadece log atılıp ACK veriliyorsa güncelleme sessizce kaybolabilir.

Veri Tutarlılığı Notları

Sprint create/start/complete local transaction içinde tutarlı olabilir

Bu olumlu.

Ama sprint assignment dağıtık olarak kırılgan olabilir

SprintService kendi state’ini güncelliyor

sonra event ile IssueService kendi state’ini uyduruyor

bu eventual consistency’den çok split write authority sorunu gibi.

Yeni issue oluşturulduktan hemen sonra sprint’e ekleme yarışı olabilir

IssueCreatedEvent projection’ı gelmeden AddIssue çağrılırsa SprintIssue not found hatası oluşabilir.

Active sprint uniqueness sadece application seviyesinde olabilir

DB-level guard yoksa concurrent start ile aynı project’te iki aktif sprint açılabilir.

Historical metrics immutable değil olabilir

Velocity anlık projection’dan hesaplanıyorsa geçmiş sprint metrikleri sonradan değişebilir.

Sprint Workflow Notları

Sprint aggregate çok ince olabilir

Start/complete dışında anlamlı planning davranışı görünmüyor olabilir.

Schedule modeli yoksa overlap analizi yapılamaz

start/end planning kavramı eksik olabilir.

Backlog → sprint geçişi sadece SprintId toggle’ı seviyesinde olabilir

capacity

freeze rule

active-only planning

commitment
gibi kurallar görünmüyor olabilir.

Sprint close sonrası incomplete issue davranışı modellenmemiş olabilir

backlog’a mı döner

sonraki sprint’e mi taşınır

snapshot mı korunur

hiçbiri net değil olabilir.

Read Model / Projection Notları

Gerçek CQRS görünmüyor olabilir

SprintIssues aynı anda backlog, sprint board ve velocity query için kullanılan projection tablosu gibi olabilir.

Read model domain’den ayrılmamış olabilir

SprintIssue bunun başlıca örneği.

Sprint progress projection eksik olabilir

Velocity sadece “şu an Done olan issue sayısı” gibi hesaplanıyorsa iş zayıf.

Completed sprint historical read model immutable değil

Sonradan gelen IssueStatusChangedEvent geçmiş sprint metriklerini değiştirebilir.

Saga / Lifecycle Notları

Sprint complete başka servisleri etkiliyorsa saga / choreography gerekebilir

Bugün sadece event publish ediliyor olabilir ama downstream orchestration görünmüyor.

Incomplete issue taşıma / backlog’a dönüş / metrics snapshot gibi süreçler modellenmemiş olabilir

Bu da ileride process manager ihtiyacına dönebilir.

Project archive/delete sprintleri etkiliyorsa lifecycle choreography şart olabilir

Şu an görünmüyor.

Güvenlik / Context Notları

Actor ve correlation bilgisi trusted context’ten gelmiyor olabilir

request body’den geliyorsa audit/orchestration güvenilirliği bozulur.

DLQ / poison message / ordering / gerçek correlation propagation eksik olabilir

Bu konu diğer servislerle ortak sistemsel tema.

SprintService için geçici hüküm

Capability olarak ayrı servis olabilir

ama şu an sprint management’den çok “sprint status + backlog projection bucket” gibi duruyor

özellikle split ownership, projection’ın domain’e sızması, workflow eksikliği ve historical mutability ciddi sorunlar

major refactor adayı

Şu ana kadar ortaklaşan sistemsel temalar daha da güçlendi

IssueService + IdentityService + ProjectService + SprintService birlikte bakınca ortak pattern’ler artık baya net:

Shared queue topology riski

Outbox güvenilirlik sorunu

Inbox/idempotency atomicity sorunu

Projection/read model ile domain model karışması

Lifecycle event propagation eksikliği

Actor/correlation trusted context eksikliği

Bazı bounded context ownership kararlarının net olmaması

DLQ / poison message / ordering stratejisi eksikliği

Bu çok iyi, çünkü finalde düzeltme planını servis bazlı değil workstream bazlı çıkarabileceğiz.