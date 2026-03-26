StorageService — Mimari İnceleme Notları
Kritik Problemler

StorageService gerçek attachment management değil, yalnız blob + minimal metadata servisi gibi duruyor

IssueId, ProjectId, EntityType gibi parent ownership/lifecycle alanları görünmüyor olabilir.

Bu yüzden attachment lifecycle dışarıda kalıyor.

Attachment orchestration client’a itilmiş olabilir

Akış:

upload

sonra issue attach

Bu iki ayrı write arasında transaction yoksa orphan file kaçınılmaz hale gelir.

Upload akışı orphan binary üretebilir

Fiziksel dosya DB save/validation’dan önce yazılıyorsa,

validation veya DB failure anında disk tarafında artık dosya kalır.

Delete akışı dangling metadata üretebilir

Önce binary silinip sonra metadata kaldırılıyorsa,

DB save fail ederse metadata dosyasız kalabilir.

File access authorization çok zayıf olabilir

Sadece [Authorize] varsa ama owner/parent entity access doğrulanmıyorsa,

herhangi bir authenticated kullanıcı başka bir dosyayı indirebilir/silebilir.

uploadedByUserId client’tan geliyor olabilir

Bu doğrudan spoofing riski.

Katmanlama Sorunları

Domain modeli çok sığ olabilir

StoredFile davranışsız metadata row gibi duruyor olabilir.

Bu kabul edilebilir ama “storage domain” iddiası zayıf kalır.

File system concern doğru yerde gibi

Infrastructure’da olması olumlu.

Messaging / outbox / inbox hiç yok olabilir

Basit blob storage için tolere edilebilir

ama attachment lifecycle bu servise bağlıysa ciddi eksik olur.

Bounded Context / Ownership Notları

Issue/Project/Comment domain bilgisi StorageService’e sızmamış gibi

Bu bounded context açısından temiz.

Ama attachment lifecycle da burada sahiplenilmiyor olabilir

Sonuç: servis ne tam object storage

ne de tam attachment context

arada kalmış “blob registry” gibi olabilir.

IssueService’te ayrı attachment bilgisi tutuluyorsa ownership sözleşmesi belirsiz

Kim owner?

kim source of truth?

kim cleanup yapacak?

açık değil olabilir.

Tek ownership alanı UploadedByUserId ise yetersiz

Parent entity ownership görünmüyor olabilir.

User/issue silinirse file lifecycle etkilenmiyor olabilir

orphan file tasarımın doğal sonucu haline gelir.

Veri Tutarlılığı Notları

Metadata + binary tutarlılığı transactional değil olabilir

Upload’da orphan binary

delete’te dangling metadata

iki yönlü consistency riski var.

Idempotent upload görünmüyor olabilir

Aynı dosya her seferinde yeni GUID ile tekrar yazılabilir.

Duplicate attachment mümkün olabilir

Issue tarafında IssueId + FileId uniqueness guard yoksa aynı dosya birkaç kez attach edilebilir.

Cleanup/reconciliation mekanizması görünmüyor olabilir

orphan cleanup job

binary-vs-metadata reconciliation
eksik olabilir.

Event / Saga Notları

StorageService event publish/consume etmiyor olabilir

FileUploaded

AttachmentDeleted

OwnerDeleted
gibi lifecycle eventleri görünmüyor olabilir.

Attachment consistency tamamen sync zincire bırakılmış olabilir

Bu da client-driven distributed transaction anlamına geliyor.

Upload → Issue attach klasik distributed transaction senaryosu

Compensation yoksa orphan file kaçınılmaz.

Delete tarafında da ters compensation gerekebilir

Parent link silinemezse binary hemen silinmemeli olabilir.

Güvenlik Notları

Owner veya parent-entity authorization eksik olabilir

Dosya erişimi sadece login olmuş olmakla korunuyorsa yetersiz.

Path güvenliği kısmen düşünülmüş olabilir

Path.GetFileName olumlu

ama canonical path doğrulaması zayıf olabilir.

Content-type / extension / malware scanning eksik olabilir

Özellikle gerçek kullanım senaryolarında kritik.

Checksum / integrity modeli görünmüyor olabilir

Dosya bozulması, tekrar yükleme, dedup gibi alanlar zayıf kalır.

Large file readiness düşük olabilir

chunk/resume/range request/CDN/presigned URL gibi özellikler yok olabilir.

Storage Abstraction Notları

IFileStorage abstraction var

Bu olumlu.

Ama abstraction çok ince olabilir

S3/blob/signed URL/multipart/range gibi daha gerçekçi provider senaryolarını taşımaya yetmeyebilir.

Pratikte local disk’e kilitli bir tasarım olabilir

soyutlama var ama yetenek seviyesi düşük olabilir.

StorageService için geçici hüküm

Binary storage concern’ini ayırma fikri doğru

ama şu an servis attachment lifecycle, ownership ve authorization açısından yetersiz

özellikle client-driven distributed transaction, orphan file riski ve zayıf authz ciddi

major refactor adayı