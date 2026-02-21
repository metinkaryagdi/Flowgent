# BitirmeProject Phase Plan (Hatirlatma)

## Vizyon
Domain-first, UX-driven microservices architecture.
Amaç: Kusursuz, tutarlı, hata toleranslı ve gerçek zamanlı bir Kanban deneyimi sunmak.
Altyapı bu deneyimi mümkün kılan araçtır.

## Phase 1 - Shared Infrastructure & Messaging Foundation (DONE)
- [x] Docker infra: RabbitMQ, Redis, Seq, PostgreSQL
- [x] Shared.Abstractions (domain base, outbox, messaging abstractions)
- [x] Shared.Common (RabbitMQ event bus, outbox publisher, DI)

## Phase 2 - Event Contracts & API Gateway & Identity JWT (DONE)
- [x] Shared.Contracts event definitions
- [x] API Gateway (YARP + JWT + routing)
- [x] Identity JWT enhancement (roles + refresh token flow)
- [ ] Refresh token migration (ERTELEDI)

## Frontend Tech Decisions (CONFIRMED)
- Web: React
- Mobile: Flutter
- Desktop: Flutter
- Priority: UI/UX quality over speed

## Phase 3 - Core Microservices + Frontend MVP (IN PROGRESS)

### Domain Authority & Projection Model
- [x] MUST: Issue Service is Single Authoritative Source for Issue State
- [x] MUST: Kanban Board is a READ MODEL / PROJECTION
- [x] MUST: Projection is stored inside Issue Service as a denormalized read model table
- SHOULD: Option to extract read model into a dedicated Query Service later (DEFER)
- [~] MUST: Eventual consistency bound for board projection is <= 500ms under normal load (LOGGING ADDED, NEEDS VALIDATION)

### Issue Service State Machine
- [x] MUST: Workflow State Machine with allowed transitions matrix
- [x] MUST: Invalid transition rejection with explicit domain error
- [x] MUST: Optimistic Concurrency Control using Version column or ETag
- [x] MUST: Conflict handling UX contract defined (HTTP 409 + refresh strategy)

### Issue Service Audit Trail
- [x] MUST: Audit log records FromStatus, ToStatus, ChangedBy, Timestamp
- [x] MUST: Audit log stored in a separate table (not full event sourcing)
- [x] MUST: Issue History API exposes audit trail for UI

### Backend - Project Service
- [x] MUST: Commands Create, Update
- [x] SHOULD: Commands Delete, AddMember, RemoveMember
- [x] SHOULD: Queries GetById, GetByUser
- [x] COULD: Queries GetTeamMembers
- [x] MUST: Events ProjectCreated
- [x] SHOULD: Events MemberAdded, ProjectSettingsUpdated
- [x] MUST: Outbox integration
- [x] MUST: Validation & error contracts (Result pattern)

### Backend - Issue Service
- [x] MUST: Commands Create, Assign, UpdateStatus
- [x] SHOULD: Commands AddComment
- [x] COULD: Commands AttachFile (metadata only)
- [x] MUST: Queries GetById, GetByProject
- [x] SHOULD: Queries GetByAssignee
- [x] COULD: Queries GetBySprint
- [x] MUST: Events IssueCreated, IssueAssigned, IssueStatusChanged
- [x] SHOULD: Event CommentAdded
- [x] MUST: NotificationRequestedEvent trigger points
- [x] MUST: Outbox integration

### Backend - Sprint Service
- [x] SHOULD: Commands Create, Start, Complete
- [x] COULD: Commands AddIssue, RemoveIssue
- [x] SHOULD: Queries GetActiveSprint
- [x] COULD: Queries GetBacklog
- [x] COULD: Queries GetSprintVelocity
- [x] SHOULD: Events SprintStarted, SprintCompleted
- [x] COULD: Event IssueAddedToSprint
- [x] SHOULD: Consumers IssueCreated, IssueStatusChanged (SprintIssue projection)
- [x] SHOULD: Outbox integration

### Backend - Notification Service
- [x] SHOULD: In-app notification model + CRUD (CREATE + GET BY USER + MARK READ)
- [x] SHOULD: SignalR hub skeleton
- [x] SHOULD: Event consumers NotificationRequested, IssueAssigned, CommentAdded, MemberAdded
- [x] COULD: Email channel (no-op sender)
- [x] SHOULD: Outbox integration (NotificationCreatedEvent, NotificationReadEvent)

### Backend - Storage Service
- [x] COULD: File upload/download/delete endpoints
- [x] COULD: Metadata tracking (PostgreSQL)
- [x] COULD: Docker volume storage-uploads

### BFF & Server-Driven UI
- [x] MUST: Introduce BFF layer for React and Flutter clients
- [x] MUST: Server-driven board configuration (columns, WIP limits, allowed transitions) (STATIC CONFIG)
- [x] MUST: Role-based UI flags delivered by API
- [x] MUST: Avoid duplicating business rules in frontend (BFF exposes workflow config)

### Frontend MVP Scope (Phase 3)
- Web (React)
- [ ] MUST: Auth flow (register/login/refresh) via Gateway `/api/v1/identity/*`
- [ ] MUST: Project list + create + update via ProjectService `/api/v1/projects/*`
- [ ] MUST: Board view via BFF `/api/v1/bff/board/{projectId}` + `/api/v1/bff/flags`
- [ ] MUST: Issue CRUD subset (create/assign/status) via IssueService `/api/v1/issues/*`
- [ ] MUST: Sprint flows (create/start/add/remove issue) via SprintService `/api/v1/sprints/*`
- [ ] SHOULD: Backlog + sprint issues list (SprintService endpoints)
- [ ] SHOULD: Attachments metadata (IssueService `/api/v1/issues/{id}/attachments`) + file upload/download (Storage `/api/v1/storage/*`)
- [ ] SHOULD: Basic navigation + role-aware route guards (flags from BFF)
- [ ] SHOULD: Notifications list (BFF `/api/v1/bff/notifications`) + SignalR in-app

- Mobile (Flutter)
- [ ] MUST: Auth flow (Gateway)
- [ ] SHOULD: Project list + board read (BFF board)
- [ ] SHOULD: Issue detail + comments read (IssueService)

- Desktop (Flutter)
- [ ] MUST: Auth flow (Gateway)
- [ ] SHOULD: Project list + board read (BFF board)
- [ ] SHOULD: Issue detail + comments read (IssueService)

### Phase 3 Acceptance Criteria (Measurable)
- MUST: Drag & drop sonrası status kesin olarak değişir ve API 200 döner
- MUST: Çakışan güncellemelerde veri kaybı olmaz, API 409 döner ve UI refresh ile toparlar
- MUST: Board projection gecikmesi <= 500ms içinde telafi edilir
- [~] MUST: Notification duplicate olmaz (idempotency + dedup) (EVENT-ID DEDUP ADDED, NEEDS VALIDATION)
- MUST: Audit trail her state değişimini kaydeder

## Phase 4 - Cross-Cutting Concerns + UX Depth (PENDING)

### Resilience & Idempotency
- [x] MUST: Idempotent consumers with MessageId tracking table (Sprint/Project/Notification/Issue done)
- MUST: Deduplication strategy (MessageId + correlation window)
- MUST: Retry policy + Dead-letter strategy
- [x] MUST: Distributed tracing (CorrelationId propagation)

### Observability Checklist (per service)
- [x] MUST: Structured logs with CorrelationId
- MUST: Error rate + latency metrics
- SHOULD: Dashboard for key endpoints (P95, error ratio)
- SHOULD: Alert thresholds for queue lag

### Backend Cross-Cutting
- [x] MUST: Serilog integration (Seq + Console)
- [x] SHOULD: Polly resilience (retry/circuit/timeout)
- [x] SHOULD: Redis caching (Project/Issue)
- [x] MUST: Health checks (per service + gateway aggregation)

### Frontend (Phase 4)
- Web (React)
- MUST: Notifications UI (in-app)
- SHOULD: Caching-aware UI states (stale/refresh)
- SHOULD: Error boundaries + global toasts
- SHOULD: Admin panels (roles/users)

- Mobile (Flutter)
- SHOULD: In-app notifications
- SHOULD: Offline-friendly UI states
- COULD: Basic profile & role view (DEFER)

- Desktop (Flutter)
- SHOULD: In-app notifications
- SHOULD: Dashboard views (project KPIs)
- COULD: Keyboard shortcuts (DEFER)

## Phase 5 - Testing, Polish & Documentation (PENDING)

### Backend Testing
- MUST: Integration tests (RabbitMQ, DB, Saga compensation)
- MUST: End-to-end flows (register → project → issue → status)
- SHOULD: Load/perf smoke tests

### Frontend Testing & Polish
- Web (React)
- SHOULD: Component tests
- SHOULD: E2E (Playwright/Cypress)
- SHOULD: Accessibility review
- SHOULD: Visual polish pass

- Mobile/Desktop (Flutter)
- SHOULD: Widget tests
- SHOULD: Integration tests
- SHOULD: Performance profiling

### Documentation
- MUST: Swagger/OpenAPI complete
- MUST: Architecture diagrams
- SHOULD: Setup/deployment guide
- SHOULD: Developer guide
- COULD: UI/UX style guide (DEFER)

## Architecture Rationale
- Domain authority in Issue Service prevents split-brain state across services
- Board projection enables fast Kanban UX without overloading write model
- Explicit consistency bound makes UX expectations measurable
- State machine + OCC protect against invalid transitions and race conditions
- Audit trail guarantees traceability and supports Issue History UI
- BFF centralizes UI rules and prevents frontend logic duplication
- Idempotency and dedup are required for reliable event-driven UX
- CorrelationId propagation enables end-to-end debugging
- Resilience strategy reduces user-visible failures under load
- MUST/SHOULD/COULD tags keep scope realistic for a solo developer
