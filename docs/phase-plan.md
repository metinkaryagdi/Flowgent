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

## Phase 3 - Core Microservices + Frontend MVP (PENDING)

### Domain Authority & Projection Model
- MUST: Issue Service is Single Authoritative Source for Issue State
- MUST: Kanban Board is a READ MODEL / PROJECTION
- MUST: Projection is stored inside Issue Service as a denormalized read model table
- SHOULD: Option to extract read model into a dedicated Query Service later (DEFER)
- MUST: Eventual consistency bound for board projection is <= 500ms under normal load

### Issue Service State Machine
- MUST: Workflow State Machine with allowed transitions matrix
- MUST: Invalid transition rejection with explicit domain error
- MUST: Optimistic Concurrency Control using Version column or ETag
- MUST: Conflict handling UX contract defined (HTTP 409 + refresh strategy)

### Issue Service Audit Trail
- MUST: Audit log records FromStatus, ToStatus, ChangedBy, Timestamp
- MUST: Audit log stored in a separate table (not full event sourcing)
- MUST: Issue History API exposes audit trail for UI

### Backend - Project Service
- MUST: Commands Create, Update
- SHOULD: Commands Delete, AddMember, RemoveMember
- SHOULD: Queries GetById, GetByUser
- COULD: Queries GetTeamMembers (DEFER if time constrained)
- MUST: Events ProjectCreated
- SHOULD: Events MemberAdded, ProjectSettingsUpdated
- MUST: Outbox integration
- MUST: Validation & error contracts (Result pattern)

### Backend - Issue Service
- MUST: Commands Create, Assign, UpdateStatus
- SHOULD: Commands AddComment
- COULD: Commands AttachFile (DEFER if time constrained)
- MUST: Queries GetById, GetByProject
- SHOULD: Queries GetByAssignee
- COULD: Queries GetBySprint (DEFER if time constrained)
- MUST: Events IssueCreated, IssueAssigned, IssueStatusChanged
- SHOULD: Event CommentAdded
- MUST: NotificationRequestedEvent trigger points
- MUST: Outbox integration

### Backend - Sprint Service
- SHOULD: Commands Create, Start, Complete
- COULD: Commands AddIssue, RemoveIssue (DEFER if time constrained)
- SHOULD: Queries GetActiveSprint
- COULD: Queries GetSprintVelocity, GetBacklog (DEFER if time constrained)
- SHOULD: Events SprintStarted, SprintCompleted
- COULD: Event IssueAddedToSprint (DEFER if time constrained)
- SHOULD: Consumers IssueCreated, IssueStatusChanged
- SHOULD: Outbox integration

### Backend - Notification Service
- SHOULD: In-app notification model + CRUD
- SHOULD: SignalR hub skeleton
- SHOULD: Event consumers NotificationRequested, IssueAssigned, CommentAdded, MemberAdded
- COULD: Email channel (DEFER)
- SHOULD: Outbox integration

### Backend - Storage Service
- COULD: File upload/download/delete endpoints (DEFER)
- COULD: Metadata tracking (PostgreSQL) (DEFER)
- COULD: Docker volume storage-uploads (DEFER)

### BFF & Server-Driven UI
- MUST: Introduce BFF layer for React and Flutter clients
- MUST: Server-driven board configuration (columns, WIP limits, allowed transitions)
- MUST: Role-based UI flags delivered by API
- MUST: Avoid duplicating business rules in frontend

### Frontend MVP Scope (Phase 3)
- Web (React)
- MUST: Auth flow (register/login/refresh)
- MUST: Project list + create + update
- MUST: Issue list + create + assign + status update
- SHOULD: Basic navigation + role-aware route guards

- Mobile (Flutter)
- MUST: Auth flow
- SHOULD: Project list + issue list (read-only)
- SHOULD: Basic issue detail view

- Desktop (Flutter)
- MUST: Auth flow
- SHOULD: Project list + issue list (read-only)
- SHOULD: Basic issue detail view

### Phase 3 Acceptance Criteria (Measurable)
- MUST: Drag & drop sonrası status kesin olarak değişir ve API 200 döner
- MUST: Çakışan güncellemelerde veri kaybı olmaz, API 409 döner ve UI refresh ile toparlar
- MUST: Board projection gecikmesi <= 500ms içinde telafi edilir
- MUST: Notification duplicate olmaz (idempotency + dedup)
- MUST: Audit trail her state değişimini kaydeder

## Phase 4 - Cross-Cutting Concerns + UX Depth (PENDING)

### Resilience & Idempotency
- MUST: Idempotent consumers with MessageId tracking table
- MUST: Deduplication strategy (MessageId + correlation window)
- MUST: Retry policy + Dead-letter strategy
- MUST: Distributed tracing (CorrelationId propagation)

### Observability Checklist (per service)
- MUST: Structured logs with CorrelationId
- MUST: Error rate + latency metrics
- SHOULD: Dashboard for key endpoints (P95, error ratio)
- SHOULD: Alert thresholds for queue lag

### Backend Cross-Cutting
- MUST: Serilog integration (Seq + Console)
- SHOULD: Polly resilience (retry/circuit/timeout)
- SHOULD: Redis caching (Project/Issue)
- MUST: Health checks (per service + gateway aggregation)

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
