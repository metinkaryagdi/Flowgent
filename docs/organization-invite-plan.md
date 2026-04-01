# Organization + Davet Sistemi — Uygulama Planı

**Branch:** `feature/organization-invite`  
**Tarih:** 2026-04-01

---

## Hedef

Manager kayıt olur → Organization kurar → çalışanları email ile davet eder.  
Her organization kendi projelerini izole görür.

---

## Etkilenen Servisler

| Servis | Değişiklik |
|--------|-----------|
| IdentityService | Organization, OrganizationMember, InviteToken — yeni tablolar + endpointler |
| ProjectService | OrganizationId kolonu eklenir, query'ler filtrelenir |
| BFF | Organization context eklenir |
| Gateway | JWT claim'e OrganizationId eklenir |
| Frontend | Kayıt sonrası org kurma, settings sayfası, davet formu |
| Issue / Sprint / Notification | DOKUNULMAZ (ProjectId üzerinden zaten izole) |

---

## Yeni Domain Modelleri (IdentityService)

### Organization
```
Id (Guid), Name (string), CreatedAt, CreatedByUserId (Guid)
```

### OrganizationMember
```
OrganizationId (Guid), UserId (Guid)
Role: Owner | Manager | Member
JoinedAt (DateTime)
```

### InviteToken
```
Id (Guid), Token (Guid - unique), Email (string)
OrganizationId (Guid), InvitedByUserId (Guid)
Role: Manager | Member   — Owner davetle atanamaz
ExpiresAt (DateTime - 48 saat), IsUsed (bool), UsedAt (DateTime?)
```

---

## Rol Hiyerarşisi

```
Owner   → Organization ayarları, üye yönetimi, tüm projeler, davet gönder
Manager → Proje oluştur, üye davet et, sprint/issue yönet
Member  → Atanan issue'ları gör/güncelle, yorum yap
```

---

## Email Altyapısı

- **Development:** MailHog (Docker) — port 1025 (SMTP), 8025 (web UI)
- **Production:** SMTP ayarları appsettings ile (Gmail / Outlook)
- `IEmailService` interface → `MailHogEmailService` / `SmtpEmailService`

---

## Yeni API Endpointleri

### OrganizationsController
```
POST   /api/v1/identity/organizations                          → CreateOrganization
GET    /api/v1/identity/organizations/my                       → GetMyOrganization
GET    /api/v1/identity/organizations/{id}/members             → GetMembers
DELETE /api/v1/identity/organizations/{id}/members/{userId}    → RemoveMember
PUT    /api/v1/identity/organizations/{id}/members/{userId}/role → ChangeRole
```

### InvitesController
```
POST   /api/v1/identity/invites                  → SendInvite (email + rol)
GET    /api/v1/identity/invites/validate/{token} → ValidateInviteToken (anonim endpoint)
POST   /api/v1/identity/invites/accept           → AcceptInvite (kayıt + org katılım birleşik)
GET    /api/v1/identity/invites/pending          → GetPendingInvites
DELETE /api/v1/identity/invites/{id}             → RevokeInvite
```

---

## JWT Claim Değişikliği

```json
{
  "org_id":   "<organizationId>",
  "org_role": "Owner | Manager | Member"
}
```

Gateway bu claim'leri downstream servislere `X-Organization-Id` header olarak iletir.

---

## ProjectService Değişikliği

- `Projects` tablosuna `OrganizationId` (Guid, nullable) eklenir
- `CreateProjectCommandHandler` → `OrganizationId` JWT claim'den otomatik alınır
- `GetProjectsByUserQueryHandler` → `OrganizationId`'ye göre filtrele
- `GetProjectByIdQueryHandler` → Organization erişim kontrolü

---

## Docker Compose Eklemesi

```yaml
mailhog:
  image: mailhog/mailhog
  ports:
    - "1025:1025"   # SMTP
    - "8025:8025"   # Web UI
```

---

## Detaylı Faz Planı

### ORG-1 — Domain Katmanı (IdentityService)

**Yeni dosyalar:**
- `IdentityService.Domain/Entities/Organization.cs`
- `IdentityService.Domain/Entities/OrganizationMember.cs`
- `IdentityService.Domain/Entities/InviteToken.cs`
- `IdentityService.Domain/Enums/OrganizationRole.cs`

**İçerik:** Entity tanımları + domain metodları  
(AddMember, RemoveMember, CreateInvite, AcceptInvite, IsExpired)

---

### ORG-2 — Application: Organization CRUD

**Yeni dosyalar:**
- `Features/Organizations/Commands/CreateOrganization/`
- `Features/Organizations/Commands/RemoveMember/`
- `Features/Organizations/Commands/ChangeMemberRole/`
- `Features/Organizations/Queries/GetMyOrganization/`
- `Features/Organizations/Queries/GetOrganizationMembers/`
- `Abstractions/IOrganizationRepository.cs`

---

### ORG-3 — Application: Davet Sistemi

**Yeni dosyalar:**
- `Features/Invites/Commands/SendInvite/`
- `Features/Invites/Commands/AcceptInvite/` ← kayıt + katılım tek handler
- `Features/Invites/Commands/RevokeInvite/`
- `Features/Invites/Queries/ValidateInviteToken/`
- `Features/Invites/Queries/GetPendingInvites/`
- `Abstractions/IInviteRepository.cs`
- `Abstractions/IEmailService.cs`

---

### ORG-4 — Infrastructure Katmanı

**Yeni dosyalar:**
- `Repositories/OrganizationRepository.cs`
- `Repositories/InviteRepository.cs`
- `Services/MailHogEmailService.cs`
- `Services/SmtpEmailService.cs`

**Değişen dosyalar:**
- `Persistence/IdentityDbContext.cs` ← yeni DbSet'ler
- Migration: `AddOrganizationTables`

---

### ORG-5 — API Katmanı

**Yeni dosyalar:**
- `Controllers/OrganizationsController.cs`
- `Controllers/InvitesController.cs`

**Değişen dosyalar:**
- `Program.cs` ← DI kayıtları

---

### ORG-6 — Kayıt Akışı + JWT Güncellemesi

**Karar:** Register sonrası frontend'de onboarding adımı (Register + CreateOrg ayrı)

**Değişen dosyalar:**
- `Security/JwtTokenGenerator.cs` ← `org_id`, `org_role` claim eklenir
- `Features/Auth/Commands/Login/LoginCommandHandler.cs` ← org claim'leri token'a ekle

---

### ORG-7 — ProjectService Güncellemesi

**Değişen dosyalar:**
- `ProjectService.Domain/Entities/Project.cs` ← `OrganizationId` eklenir
- `Features/Projects/Commands/CreateProject/CreateProjectCommandHandler.cs`
- `Features/Projects/Queries/GetProjectsByUser/GetProjectsByUserQueryHandler.cs`
- `Features/Projects/Queries/GetProjectById/GetProjectByIdQueryHandler.cs`
- Migration: `AddOrganizationIdToProjects`

---

### ORG-8 — Gateway & BFF Güncelleme

**Gateway:** `X-Organization-Id` header forwarding  
**BFF:** Organization context response model'lerine eklenir

---

### ORG-9 — Frontend: Onboarding Akışı

**Yeni sayfalar:**
- `/onboarding` — kayıt sonrası organization kurma adımı
- `/invite/accept?token=xxx` — davet kabul + kayıt formu

---

### ORG-10 — Frontend: Organization Settings

**Yeni sayfa:** `/settings/organization`
- Organization adı (düzenlenebilir)
- Üye listesi (rol badge'leri)
- Üye davet formu (email + rol seç)
- Bekleyen davetler + iptal butonu
- Üye çıkarma

---

### ORG-11 — Unit Testler

- `OrganizationRepository` testleri
- `SendInviteCommandHandler` testleri
- `AcceptInviteCommandHandler` testleri
- JWT org claim doğrulama testleri

---

### ORG-12 — Docker: MailHog Entegrasyonu

- `docker-compose.yml` güncelleme
- `appsettings.Development.json` / `appsettings.Production.json` ayrımı

---

## Bağımlılık Sırası

```
ORG-1 → ORG-2 → ORG-3 → ORG-4 → ORG-5 → ORG-6
                                              ↓
                                           ORG-7 → ORG-8
                                              ↓
                                   ORG-9 → ORG-10 → ORG-11 → ORG-12
```
