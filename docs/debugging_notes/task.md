# BitirmeProject - Faz Bazli Duzeltme Plani

## FAZ 1 - Foundation / Messaging Reliability
- [x] 1.1 RabbitMQ topology duzelt (subscriber-specific queue naming)
- [x] 1.2 OutboxMessage modeli guclendir (LockId, ClaimedUntil, NextRetryAt, PublishedOn)
- [x] 1.3 OutboxPublisherService - optimistic claim/lock mekanizmasi ekle
- [x] 1.4 Inbox/ProcessedEvent - domain'den cikar, Shared.Common'a tasi (InboxEntry + IInboxRepository olusturuldu)
- [x] 1.5 Consumer'larda atomic inbox: handler + processedEvent ayni SaveChanges
- [x] 1.6 DLQ (Dead Letter Queue) ve dead-letter exchange tanimla
- [x] 1.7 CorrelationId / ActorId header standardi - publish/consume pipeline

## FAZ 2 - Trust & Security Corrections
- [x] 2.1 IdentityService: endpoint authorization ayrimi (public auth vs admin)
- [x] 2.2 IdentityService: body'den user identity alimni kaldir, Claims'den al
- [x] 2.3 IdentityService: email/username normalization (LowerInvariant)
- [x] 2.4 IdentityService: refresh token hashing (SHA-256)
- [x] 2.5 StorageService: uploadedByUserId claims'ten turet
- [x] 2.6 StorageService: download/delete icin ownership dogrulama
- [x] 2.7 NotificationService: GetByUser icin claims-based userId dogrulama
- [x] 2.8 ProjectService: route/body OwnerUserId yerine claims

## FAZ 3 - Ownership & Boundary Cleanup
- [x] 3.1 Sprint assignment ownership karari -> SprintService owner (karar belgesi + kod)
- [x] 3.2 Issue.SprintId -> read-only projection alanina donustur veya kaldir
- [x] 3.3 Project aggregate: IssueCount/OpenIssueCount -> ProjectSummary read model
- [x] 3.4 NotificationService: policy ownership netlestir (Model 1: NS kendisi policy cikarir)
- [x] 3.5 StorageService: saf blob + minimal metadata service olarak tanimla

## FAZ 4 - Domain Modeling Fixes
- [x] 4.1 Sprint: StartDate, EndDate, Goal alanlari ekle
- [x] 4.2 Sprint: active sprint uniqueness - DB constraint ekle
- [x] 4.3 Sprint: CompletedSprint immutable rule
- [x] 4.4 Sprint: close sonrasi carry-over politikasi (backlog/next-sprint/manual)
- [x] 4.5 ProjectMember: Owner/Admin/Member role ayrimi ekle
- [x] 4.6 Project: owner'i member olarak da kaydet
- [ ] 4.7 Issue: IssueBoardItem -> projection'a tasi (domain'den cikar)
- [x] 4.8 NotificationService: Queued/Sent/Failed/Delivered delivery lifecycle
- [x] 4.9 IdentityService: User aggregate guclendir (FailedLoginCount, LockoutEnd, SecurityStamp)

## FAZ 5 - Attachment & Notification Lifecycle Stabilization
- [ ] 5.1 StorageService: temp upload + finalize akis (Yol A)
- [ ] 5.2 StorageService: orphan binary cleanup job
- [ ] 5.3 NotificationService: delivery'yi handler'dan cikar -> delivery worker
- [ ] 5.4 NotificationService: delivery state vs consumption state ayrimi

## FAZ 6 - Production Hardening & Observability
- [ ] 6.1 Structured logging standardi (correlationId, actorId, entityId, eventId, consumerName)
- [ ] 6.2 Health check: outbox worker, DLQ depth, failed delivery count
- [ ] 6.3 Event schema / version metadata (EventVersion alani)
- [ ] 6.4 Projection replay/rebuild stratejisi (tasarim belgesi)
