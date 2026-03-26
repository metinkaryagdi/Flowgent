# Faz 3 Ownership Kararlari

## 1. Sprint assignment owner: SprintService

Karar:
`SprintService`, issue'nin hangi sprintte oldugunun tek write-owner'idir.

Kod etkisi:
- `IssueService.Domain.Entities.Issue` icinden `SprintId` kaldirildi.
- `IssueAddedToSprintEvent` ve `IssueRemovedFromSprintEvent` artik yalnizca `IssueBoardItem` projection'ini guncelliyor.
- `IssueDto.SprintId` artik write-state degil, board/read projection uzerinden dolduruluyor.

Sonuc:
- Ayni gercegin iki ayri write modelde tutulmasi bitti.
- `IssueService` sprint bilgisini sorgu/projection seviyesinde goruyor.

## 2. Project issue summary owner: ProjectSummary read model

Karar:
`Project` aggregate issue sayaclarini tasimaz; bu bilgiler `ProjectSummary` read modelinde tutulur.

Kod etkisi:
- `Project` aggregate icinden `IssueCount`, `OpenIssueCount`, `InProgressIssueCount`, `DoneIssueCount` alanlari kaldirildi.
- Yeni `ProjectSummary` projection'i eklendi.
- `IssueCreatedEvent` ve `IssueStatusChangedEvent` consumer'lari artik `ProjectSummary` projection'ini guncelliyor.
- `ProjectDto` ayni alanlari koruyor, fakat veriyi artik projection'dan topluyor.

Sonuc:
- Project write modeli lifecycle ve sahiplik icin daha temiz kaldı.
- UI/API kontrati bozulmadan bounded-context temizligi saglandi.

## 3. Notification policy owner: NotificationService

Karar:
Upstream servisler bildirim istemez; business event yayinlar. Notification policy'yi `NotificationService` cikarir.

Kod etkisi:
- `IssueService` icindeki `NotificationRequestedEvent` publish yolu issue status degisiminden kaldirildi.
- `NotificationService` icine yeni `IssueStatusChangedEventHandler` eklendi.
- `NotificationEventsConsumer`, `IssueStatusChangedEvent` dinliyor; `NotificationRequestedEvent` runtime kaydi cikartildi.

Sonuc:
- NotificationService daha tutarli bir policy owner oldu.
- Upstream tarafinda command-kiligi event akisi azaltildi.

## 4. Storage boundary: pure blob + minimal metadata

Karar:
`StorageService` attachment relation owner degildir; yalnizca blob ve minimal dosya metadata'si owner'idir.

Kod etkisi:
- `StoredFile` modeli bu siniri aciklayan not ile netlestirildi.
- Issue/Project/Comment iliskileri Storage tarafina tasinmadi.

Sonuc:
- Attachment lifecycle ownership'i baska bounded context'lerde kalir.
- Storage servisi sade blob registry olarak tanimlanmis oldu.
