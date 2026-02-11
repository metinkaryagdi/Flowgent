# Next Phases - Quick Reference Plan

## Phase 2: Event Contracts & API Gateway (Tahmini: 3-4 saat)

### Shared.Contracts - Event Definitions
```
Priority: HIGH
Files to Create: ~10 event classes
```

**Events:**
- `ProjectCreatedEvent`
- `ProjectUpdatedEvent`
- `IssueCreatedEvent`
- `IssueAssignedEvent`
- `IssueStatusChangedEvent`
- `CommentAddedEvent`
- `SprintStartedEvent`
- `SprintCompletedEvent`
- `NotificationRequestedEvent`
- Saga compensation events

**Her event şunları içermeli:**
- `Guid EventId`
- `DateTime OccurredOn`
- `Guid CorrelationId`
- Domain-specific properties

---

### API Gateway with YARP
```
Priority: HIGH
Estimated Time: 2 hours
```

**Oluşturulacaklar:**
1. `src/services/gateway/ApiGateway` projesi
2. YARP configuration (appsettings.json)
3. JWT authentication middleware
4. CORS policies
5. Rate limiting (optional)
6. Health check aggregation

**Routing Pattern:**
```json
/api/v1/identity/*     → identity-api:8080
/api/v1/projects/*     → project-api:8080
/api/v1/issues/*       → issue-api:8080
/api/v1/sprints/*      → sprint-api:8080
/api/v1/notifications/* → notification-api:8080
/api/v1/storage/*      → storage-api:8080
```

**Package References:**
- `Yarp.ReverseProxy (2.2.0)`
- `Microsoft.AspNetCore.Authentication.JwtBearer (9.0.0)`

---

### Identity Service JWT Enhancement
```
Priority: HIGH
Estimated Time: 1-2 hours
```

**Eklenecekler:**
1. JWT token generation
2. User registration/login endpoints
3. Password hashing (BCrypt)
4. Role management (Admin, ProjectManager, Developer, Viewer)
5. Refresh token support (optional)

**appsettings.json:**
```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyMinimum32Characters",
    "Issuer": "BitirmeProject.IdentityService",
    "Audience": "BitirmeProject.Clients",
    "ExpirationMinutes": 60
  }
}
```

---

## Phase 3: Core Microservices (Tahmini: 10-15 saat)

### Project Service
```
Priority: HIGH
Estimated Time: 3-4 hours
Database: project-db (PostgreSQL)
```

**Domain Models:**
- `Project` (AggregateRoot)
- `Team`
- `ProjectMember`
- `ProjectSettings`

**Features (CQRS):**
- Commands: Create, Update, Delete, AddMember, RemoveMember
- Queries: GetById, GetByUser, GetTeamMembers

**Events:**
- `ProjectCreatedEvent`
- `MemberAddedEvent`
- `ProjectSettingsUpdatedEvent`

**Outbox Integration:** ✅ Required

---

### Issue Service
```
Priority: HIGH
Estimated Time: 4-5 hours
Database: issue-db (PostgreSQL)
```

**Domain Models:**
- `Issue` (AggregateRoot)
- `Comment`
- `Attachment` (storage-service ile integrate)
- `IssueType`
- `IssueStatus`

**Features (CQRS):**
- Commands: Create, Assign, UpdateStatus, AddComment, AttachFile
- Queries: GetById, GetByProject, GetBySprint, GetByAssignee

**Events:**
- `IssueCreatedEvent`
- `IssueAssignedEvent` → triggers NotificationRequestedEvent
- `IssueStatusChangedEvent`
- `CommentAddedEvent` → triggers NotificationRequestedEvent

**Outbox Integration:** ✅ Required

---

### Sprint Service
```
Priority: MEDIUM
Estimated Time: 3-4 hours
Database: sprint-db (PostgreSQL)
```

**Domain Models:**
- `Sprint` (AggregateRoot)
- `BacklogItem`
- `Velocity`

**Features (CQRS):**
- Commands: Create, Start, Complete, AddIssue, RemoveIssue
- Queries: GetActiveSprint, GetSprintVelocity, GetBacklog

**Events:**
- `SprintStartedEvent`
- `SprintCompletedEvent`
- `IssueAddedToSprintEvent`

**Event Consumers:**
- `IssueCreatedEvent` listener
- `IssueStatusChangedEvent` listener

**Outbox Integration:** ✅ Required

---

### Notification Service
```
Priority: MEDIUM
Estimated Time: 3-4 hours
Database: notification-db (PostgreSQL)
```

**Domain Models:**
- `Notification` (AggregateRoot)
- `NotificationTemplate`

**Features:**
- Multiple channels: In-App, Email (future: Push)
- SignalR hub for real-time
- Notification history
- Mark as read

**SignalR Hub:**
```csharp
public class NotificationHub : Hub
{
    public async Task JoinUserGroup(string userId);
    public async Task LeaveUserGroup(string userId);
}
```

**Event Consumers:**
- `NotificationRequestedEvent`
- `IssueAssignedEvent`
- `CommentAddedEvent`
- `MemberAddedEvent`

**SignalR + Redis Backplane:**
```csharp
services.AddSignalR()
    .AddStackExchangeRedis("redis:6379,password=redis123");
```

**Outbox Integration:** ✅ Required

---

### Storage Service
```
Priority: LOW
Estimated Time: 2-3 hours
Database: storage-db (PostgreSQL)
```

**Domain Models:**
- `StoredFile` (AggregateRoot)
- `FileMetadata`

**Features:**
- File upload with validation
- File download with streaming
- File deletion
- Metadata tracking

**Storage Strategy:**
```
Files: /app/uploads/{EntityType}/{EntityId}/filename.ext
Metadata: PostgreSQL
```

**API Endpoints:**
```
POST /api/v1/storage/upload
GET /api/v1/storage/{fileId}
DELETE /api/v1/storage/{fileId}
GET /api/v1/storage/metadata/{fileId}
```

**Docker Volume:**
```yaml
storage-api:
  volumes:
    - storage-uploads:/app/uploads
```

---

## Phase 4: Cross-Cutting Concerns (Tahmini: 4-6 saat)

### Serilog Integration
```
Priority: MEDIUM
All Services
```

**Package:**
```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
```

**Configuration:**
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "ProjectService")
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();
```

---

### Polly Resilience
```
Priority: MEDIUM
All inter-service communication
```

**Patterns:**
- Circuit Breaker
- Retry with exponential backoff
- Timeout policies

```csharp
services.AddHttpClient("ProjectService")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());
```

---

### Redis Caching
```
Priority: LOW
High-traffic services (Project, Issue)
```

**Implementation:**
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379,password=redis123";
    options.InstanceName = "ProjectService_";
});
```

**Cache Keys:**
- `ProjectService_Project_{id}`
- `IssueService_Issue_{id}`
- `IssueService_ProjectIssues_{projectId}`

---

### Health Checks
```
Priority: MEDIUM
All services
```

**Per Service:**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq")
    .AddRedis(redisConnectionString, name: "redis");
```

**API Gateway Aggregation:**
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

---

## Phase 5: Testing & Documentation (Tahmini: 3-4 saat)

### Integration Tests
- RabbitMQ event flow tests
- Saga compensation tests
- Database integration tests with TestContainers

### End-to-End Tests
- User registration → Project creation → Issue creation → Sprint planning flow
- Notification delivery test

### Documentation
- API documentation (Swagger/OpenAPI)
- Architecture diagrams (Mermaid)
- Setup/deployment guide
- Developer guide

---

## Toplam Tahmini Süre

| Phase | Süre |
|-------|------|
| Phase 2 | 3-4 saat |
| Phase 3 | 10-15 saat |
| Phase 4 | 4-6 saat |
| Phase 5 | 3-4 saat |
| **TOPLAM** | **20-29 saat** |

---

## Öncelik Sırası (Devam İçin)

1. ✅ **Phase 1** - TAMAMLANDI
2. 🔄 **Phase 2.1** - Shared.Contracts (Event definitions)
3. 🔄 **Phase 2.2** - API Gateway (YARP)
4. 🔄 **Phase 2.3** - Identity JWT enhancement
5. 🔄 **Phase 3.1** - Project Service
6. 🔄 **Phase 3.2** - Issue Service
7. 🔄 **Phase 3.3** - Notification Service + SignalR
8. 🔄 **Phase 3.4** - Sprint Service
9. 🔄 **Phase 3.5** - Storage Service
10. 🔄 **Phase 4** - Cross-cutting concerns
11. 🔄 **Phase 5** - Testing & Documentation

---

## Sonraki Oturum İçin Checklist

**Başlamadan Önce:**
- [ ] `docker-compose up -d rabbitmq redis identity-db` (Seq opsiyonel)
- [ ] Verify RabbitMQ UI: http://localhost:15672
- [ ] Shared libraries build check

**İlk Yapılacak:**
- [ ] Shared.Contracts event class'larını oluştur
- [ ] API Gateway projesi oluştur
- [ ] YARP routing configuration

**Beklenen Output:**
- Event contracts hazır
- API Gateway ile routing çalışıyor
- Identity Service JWT token üretiyor

---

**Son Güncelleme:** 22 Aralık 2025  
**Mevcut Durum:** Phase 1 Complete ✅
