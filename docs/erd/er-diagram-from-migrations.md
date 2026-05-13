# ER Diyagramı — EF Core Migration Snapshot'larından

> **Kaynak:** Her servisin `*ModelSnapshot.cs` dosyaları (gerçek DB şeması).
> Bu dosya `er-diagram.md`'den daha **otoriter**dir; çünkü gerçek migration metadata'sından üretilmiştir.
> Read model'leri (CQRS), Outbox ve ProcessedEvents altyapı tablolarını da içerir.

## Render

```
mermaid.live → erDiagram bloğunu yapıştır → SVG indir
```

## Önemli Notlar

- **Servisler arası fiziksel FK YOKTUR.** `ProjectId`, `UserId`, `OrganizationId` gibi alanlar logical FK'dir (sadece UUID referansı).
- **Read model'ler** (`IssueBoardItem`, `SprintIssue`) CQRS pattern'inin write/read ayrımı için denormalize edilmiş tablolardır. Event'lerle güncellenir.
- **Outbox + ProcessedEvents** altyapı tablolarıdır (transactional outbox pattern, event idempotency).
- **Soft delete:** Identity entity'leri `IsDeleted` + `DeletedAt` alanlarına sahiptir; unique index'ler `IsDeleted=FALSE` filter'ı ile çalışır.

---

## 1. IdentityService DB

```mermaid
erDiagram
    Users ||--o{ UserRoles : "has"
    Roles ||--o{ UserRoles : "assigned_to"
    Users ||--o{ refresh_tokens : "owns"
    Organizations ||--o{ OrganizationMembers : "has"
    Users ||--o{ OrganizationMembers : "belongs_to"
    Organizations ||--o{ InviteTokens : "issues"

    Users {
        uuid Id PK
        varchar UserName "UK partial(IsDeleted=false), max 50"
        varchar Email "UK partial(IsDeleted=false), max 100"
        text PasswordHash
        int Status
        int FailedLoginCount
        timestamptz LockoutEnd "nullable"
        uuid SecurityStamp
        timestamptz PasswordChangedAt "nullable"
        uuid LastActiveOrganizationId "nullable"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
        timestamptz DeletedAt "nullable"
        boolean IsDeleted "default false"
    }

    Roles {
        uuid Id PK
        varchar Name UK "max 50"
        varchar Description "nullable, max 200"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
        timestamptz DeletedAt "nullable"
        boolean IsDeleted
    }

    UserRoles {
        uuid UserId PK,FK
        uuid RoleId PK,FK
    }

    refresh_tokens {
        uuid Id PK
        uuid UserId FK
        varchar Token UK "max 200"
        timestamptz ExpiresAt
        timestamptz RevokedAt "nullable"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
        timestamptz DeletedAt "nullable"
        boolean IsDeleted
    }

    Organizations {
        uuid Id PK
        varchar Name "max 100"
        uuid CreatedByUserId "logical FK -> Users.Id"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
        timestamptz DeletedAt "nullable"
        boolean IsDeleted "default false"
    }

    OrganizationMembers {
        uuid OrganizationId PK,FK
        uuid UserId PK,FK
        varchar Role "max 20, enum string"
        timestamptz JoinedAt
    }

    InviteTokens {
        uuid Id PK
        uuid Token UK
        varchar Email "max 100"
        uuid OrganizationId FK
        uuid InvitedByUserId "logical FK -> Users.Id"
        varchar Role "max 20, enum string"
        timestamptz ExpiresAt
        boolean IsUsed "default false"
        timestamptz UsedAt "nullable"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
        timestamptz DeletedAt "nullable"
        boolean IsDeleted "default false"
    }

    OutboxMessages {
        uuid Id PK
        text EventType
        text Payload
        timestamptz OccurredOn
        timestamptz PublishedOn "nullable"
        int Status
        int RetryCount
        timestamptz NextRetryAt "nullable"
        timestamptz LastAttemptedAt "nullable"
        text LastError "nullable"
        uuid LockId "nullable"
        timestamptz ClaimedUntil "nullable"
        text CorrelationId "nullable"
        text ActorId "nullable"
    }
```

---

## 2. ProjectService DB

```mermaid
erDiagram
    Projects ||--|| ProjectSummaries : "summarized_by"
    Projects ||--o{ ProjectMembers : "has"

    Projects {
        uuid Id PK
        varchar Name "max 200"
        varchar Key UK "max 10, e.g. PROJ"
        uuid OwnerUserId "logical FK -> Identity.Users"
        uuid OrganizationId "nullable, logical FK"
        boolean IsArchived
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    ProjectMembers {
        uuid ProjectId PK,FK
        uuid UserId PK "logical FK -> Identity.Users"
        uuid AddedByUserId "logical FK -> Identity.Users"
        int Role "ProjectMemberRole enum"
        timestamptz AddedAt
    }

    ProjectSummaries {
        uuid ProjectId PK,FK
        int IssueCount
        int OpenIssueCount
        int InProgressIssueCount
        int DoneIssueCount
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    ProcessedEvents {
        uuid EventId PK,UK
        varchar EventType "max 200"
        timestamptz ProcessedOn
    }

    OutboxMessages {
        uuid Id PK
        text EventType
        text Payload
        timestamptz OccurredOn
        timestamptz PublishedOn
        int Status
    }
```

---

## 3. IssueService DB

> **CQRS:** `Issues` = write model (aggregate root), `IssueBoardItems` = read model (board görüntüsü için denormalize, `SprintId` içerir).

```mermaid
erDiagram
    Issues ||--o{ IssueComments : "has"
    Issues ||--o{ IssueAttachments : "has"
    Issues ||--o{ IssueAudits : "tracked_by"

    Issues {
        uuid Id PK
        uuid ProjectId "indexed, logical FK"
        uuid OrganizationId "nullable, logical FK"
        varchar Title "max 200"
        varchar Description "nullable, max 2000"
        int Status "IssueStatus enum"
        int Priority "IssuePriority enum"
        uuid CreatedByUserId "logical FK"
        uuid AssigneeUserId "nullable, logical FK"
        int Version "concurrency token, default 1"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    IssueComments {
        uuid Id PK
        uuid IssueId FK "indexed"
        uuid AuthorUserId "logical FK"
        varchar Content "max 2000"
        timestamptz CreatedAt
    }

    IssueAttachments {
        uuid Id PK
        uuid IssueId FK "indexed"
        uuid FileId "indexed, logical FK -> Storage.StoredFiles"
        varchar FileName "max 255"
        varchar ContentType "max 200"
        bigint SizeBytes
        uuid UploadedByUserId "logical FK"
        timestamptz UploadedAt
    }

    IssueAudits {
        uuid Id PK
        uuid IssueId FK "indexed"
        int FromStatus
        int ToStatus
        uuid ChangedByUserId "logical FK"
        timestamptz ChangedAt
    }

    IssueBoardItems {
        uuid IssueId PK "denormalized read model"
        uuid ProjectId "indexed"
        uuid SprintId "nullable, indexed, logical FK -> Sprint"
        uuid OrganizationId "nullable"
        varchar Title "max 200"
        int Status
        int Priority
        uuid AssigneeUserId "nullable"
        int Version
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    ProcessedEvents {
        uuid EventId PK,UK
        varchar EventType
        timestamptz ProcessedOn
    }

    OutboxMessages {
        uuid Id PK
        text EventType
        text Payload
    }
```

---

## 4. SprintService DB

> **İş kuralı:** `Sprints` üzerinde `(ProjectId)` unique partial index var, `Status=1 (Active)` filter'lı — bir projede **aynı anda yalnızca 1 aktif sprint** olabilir.

```mermaid
erDiagram
    Sprints ||--o| SprintSummaries : "completion_snapshot"

    Sprints {
        uuid Id PK
        uuid ProjectId "UK partial(Status=Active), logical FK"
        uuid OrganizationId "nullable, logical FK"
        varchar Name "max 200"
        varchar Goal "nullable, max 2000"
        timestamptz StartDate
        timestamptz EndDate
        int Status "Planned/Active/Completed"
        uuid CreatedByUserId "logical FK"
        timestamptz StartedAt "nullable"
        timestamptz CompletedAt "nullable"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    SprintSummaries {
        uuid SprintId PK,UK "immutable snapshot on completion"
        int TotalIssues
        int CompletedIssues
        timestamptz CompletedAt
        timestamptz SnapshotTakenAt
    }

    SprintIssues {
        uuid IssueId PK "denormalized read model"
        uuid SprintId "nullable, indexed, logical FK"
        uuid ProjectId "indexed"
        uuid OrganizationId "nullable, indexed"
        uuid CreatedByUserId
        varchar Title "max 200"
        varchar Status "max 50, string enum"
        varchar Priority "max 50, string enum"
        varchar IssueType "max 100"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    ProcessedEvents {
        uuid EventId PK,UK
        varchar EventType
        timestamptz ProcessedOn
    }

    OutboxMessages {
        uuid Id PK
        text EventType
        text Payload
    }
```

---

## 5. NotificationService DB

```mermaid
erDiagram
    Notifications {
        uuid Id PK
        uuid UserId "indexed, logical FK"
        varchar Title "max 200"
        varchar Message "max 2000"
        int Channel "InApp/Email"
        int Status "indexed, Queued/Sent/Delivered/Failed"
        boolean IsRead
        timestamptz ReadAt "nullable"
        int DeliveryAttemptCount
        timestamptz LastDeliveryAttemptAt "nullable"
        timestamptz NextDeliveryAttemptAt "indexed, nullable"
        timestamptz DeliveredAt "nullable"
        varchar LastFailureReason "nullable, max 2000"
        varchar EntityType "nullable, max 100"
        uuid EntityId "nullable"
        uuid ExternalEventId "indexed, nullable"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    ProcessedEvents {
        uuid EventId PK,UK
        varchar EventType
        timestamptz ProcessedOn
    }

    OutboxMessages {
        uuid Id PK
        text EventType
        text Payload
    }
```

---

## 6. StorageService DB

```mermaid
erDiagram
    StoredFiles {
        uuid Id PK
        varchar FileName "max 255"
        varchar ContentType "max 200"
        bigint SizeBytes
        varchar StoragePath "max 500"
        uuid UploadedByUserId "indexed, logical FK"
        int Status "indexed, Temporary/Finalized"
        timestamptz UploadedAt
        timestamptz ExpiresAt "indexed, nullable"
        timestamptz FinalizedAt "nullable"
    }
```

---

## 7. AiService DB

```mermaid
erDiagram
    AiSessions ||--o{ AiPlanResults : "produces"
    AiSessions ||--o{ AiToolExecutions : "executes"

    AiSessions {
        uuid Id PK
        uuid ProjectId "logical FK"
        uuid UserId "logical FK"
        uuid OrganizationId "logical FK"
        text Type "string enum"
        text Status "string enum: Pending/Processing/Completed/Failed"
        timestamptz CompletedAt "nullable"
        varchar ErrorMessage "nullable, max 1000"
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    AiPlanResults {
        uuid Id PK
        uuid SessionId FK "indexed"
        text Prompt
        text RawResponse
        text ParsedJson "nullable"
        boolean WasApplied
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }

    AiToolExecutions {
        uuid Id PK
        uuid SessionId "nullable, indexed"
        uuid UserId "logical FK"
        uuid OrganizationId "indexed composite"
        uuid ProjectId "indexed composite"
        varchar ToolName "max 64"
        text InputJson
        text OutputJson "nullable"
        boolean Success
        varchar ErrorMessage "nullable, max 2000"
        bigint DurationMs
        timestamptz CreatedAt
        timestamptz UpdatedAt "nullable"
    }
```

---

## Cross-Service Logical FK Haritası

> Bu ilişkiler **veritabanı seviyesinde değil**, sadece UUID referansıyla mantıksal olarak vardır. Mikroservis bağımsızlığını korur.

```mermaid
erDiagram
    Identity_Users ||..o{ Project_Projects : "owns"
    Identity_Users ||..o{ Project_ProjectMembers : "is"
    Identity_Organizations ||..o{ Project_Projects : "scopes"

    Identity_Users ||..o{ Issue_Issues : "creates/assigned"
    Identity_Organizations ||..o{ Issue_Issues : "scopes"
    Project_Projects ||..o{ Issue_Issues : "contains"

    Identity_Users ||..o{ Sprint_Sprints : "creates"
    Identity_Organizations ||..o{ Sprint_Sprints : "scopes"
    Project_Projects ||..o{ Sprint_Sprints : "contains"

    Sprint_Sprints ||..o{ Issue_IssueBoardItems : "groups (read model)"
    Issue_Issues ||..o{ Sprint_SprintIssues : "denormalized into"

    Identity_Users ||..o{ Notification_Notifications : "receives"

    Identity_Users ||..o{ Storage_StoredFiles : "uploads"
    Storage_StoredFiles ||..o{ Issue_IssueAttachments : "referenced_by"

    Identity_Users ||..o{ Ai_AiSessions : "starts"
    Project_Projects ||..o{ Ai_AiSessions : "scoped_to"
    Identity_Organizations ||..o{ Ai_AiSessions : "scoped_to"
```

---

## EFCorePowerTools ile Görsel Çıktı (Visual Studio)

Eğer Mermaid yerine **Visual Studio içinden** ER diyagramı çıkarmak istersen:

1. **EF Core Power Tools** eklentisini Visual Studio'ya yükle: VS → Extensions → Manage Extensions → "EF Core Power Tools" ara → Install
2. Solution Explorer'da her servisin `*.Infrastructure` projesine **sağ tık** → **EF Core Power Tools** → **Add DbContext Diagram**
3. Açılan pencerede DbContext'i seç → **OK** → `.dgml` dosyası oluşur
4. `.dgml` dosyasına çift tıkla → VS otomatik render eder → sağ tık **Save As Image** (PNG)

**7 servis için tekrarla:**
- IdentityService.Infrastructure
- ProjectService.Infrastructure
- IssueService.Infrastructure
- SprintService.Infrastructure
- NotificationService.Infrastructure
- StorageService.Infrastructure
- AiService.Infrastructure

Çıktılar `docs/erd/dgml/` klasörüne taşınabilir.
