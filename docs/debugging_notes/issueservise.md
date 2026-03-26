IssueService — Mimari İnceleme Notları
Kritik Problemler

Shared queue topology riski var

Queue isimleri event tipine göre ortaklaşıyorsa pub/sub bozulur.

Her consumer’ın kendi queue’su olması gerekebilir.

Bu konu diğer servislerde de özellikle kontrol edilecek.

Outbox güvenilir görünmüyor

Publish sonrası state gerçekten persist ediliyor mu doğrulanmalı.

Outbox varmış gibi ama fiilen broken olabilir.

Inbox/idempotency atomic olmayabilir

ProcessedEvent yaklaşımı var ama yarış durumu riski bulunuyor.

Business state ile processed-event kaydı aynı güvenli sınırda mı bakılacak.

DLQ / poison message yönetimi eksik olabilir

requeue: false varsa mesaj kaybı riski var.

DLQ tasarımı ayrıca incelenecek.

Katmanlama Sorunları

IssueBoardItem domain’de durmamalı

Bu bir read model / projection adayı.

Domain’den çıkarılması gerekebilir.

ProcessedEvent domain concern değil

Bu inbox / messaging / infrastructure concern’i.

Domain’den ayrılmalı.

Domain event / integration event ayrımı bulanık

Aggregate seviyesinde kavramsal ayrım net değil.

Projection update mantığı dağınık olabilir

Manuel sync edilen read model drift riski oluşturuyor.

Bounded Context / Ownership Soruları

SprintId ownership net değil

IssueService mi owner?

SprintService mi owner?

Şimdilik karar verilmedi, diğer servisleri gördükten sonra netleşecek.

Notification policy IssueService’e sızmış olabilir

Notification kararları IssueService’te olmamalı olabilir.

NotificationService’in sorumluluğu yeniden değerlendirilecek.

Event Tasarımı Notları

Event contract’lar zayıf olabilir

Özellikle downstream servislerin tekrar HTTP çağrısı yapma ihtiyacı doğuyorsa.

Bazı event payload’larında hardcoded / sahte alan olabilir

Örn. domain’de olmayan ama event’te geçen alanlar.

Versioning / ordering metadata ihtiyacı olabilir

Retry, replay ve stale update tespiti için.

Güvenlik / Güvenilirlik Notları

CorrelationId propagation net incelenecek

Handler’lar doğru correlation taşıyor mu?

Actor bilgisinin trusted context’ten gelmesi gerekebilir

Request body’den alınıyorsa riskli.

FK / relational consistency zayıf olabilir

Aynı servis DB’sindeki bağlı tablolar için ayrıca bakılacak.

DDD Notları

Issue entity kötü değil, ama henüz tam güçlü DDD değil

Behavior var

ama invariant derinliği sınırlı

primitive obsession var

value object kullanımı görünmüyor

Comment / Attachment / Audit aggregate sınırları netleşmeli

Aynı aggregate mi?

aynı servis içinde ayrı aggregate mi?

sonraki plan aşamasında karar verilecek

Şimdilik IssueService için geçici hüküm

Temel fikir yanlış değil

uygulama seviyesi riskler yüksek

özellikle EDA reliability tarafı zayıf olabilir

major refactor adayı ama karar tüm servisler görüldükten sonra kesinleşecek