# Data protection — what Flowgent stores, where, and for how long

This is the technical inventory behind a privacy notice: which personal data the system
holds, which component holds it, and what happens when a user asks to be deleted. It is
not legal advice and does not attempt to be a privacy policy — it is the input someone
writing one would need.

Last verified against the running stack: 2026-08-05.

## 1. What counts as personal data here

Only two columns in the entire system identify a person by themselves:

| Data | Where | Why it exists |
|---|---|---|
| Email address | `identitydb.Users.Email` | Sign-in identifier, verification and invite delivery |
| Email address | `identitydb.InviteTokens.Email` | An invite is addressed before an account exists |
| Email address | `identitydb.email_verification_tokens.Email` | Pins a token to the address it was issued for |
| Username | `identitydb.Users.UserName` | Display name and alternative sign-in identifier |

Everything else about a person is a `UserId` GUID. **No other service stores a name or an
address** — project, issue, sprint, notification and storage databases hold only the GUID:

| Service | Columns referencing a user |
|---|---|
| project | `Projects.OwnerUserId`, `ProjectMembers.UserId`, `ProjectMembers.AddedByUserId` |
| issue | `Issues.CreatedByUserId`, `Issues.AssigneeUserId`, `IssueComments.AuthorUserId`, `IssueAudits.ChangedByUserId`, `IssueAttachments.UploadedByUserId`, `IssueBoardItems.AssigneeUserId` |
| sprint | `Sprints.CreatedByUserId`, `SprintIssues.CreatedByUserId` |
| notification | `Notifications.UserId` |
| storage | `StoredFiles.UploadedByUserId` |

That split is what makes erasure tractable: remove the two identifying columns and every
remaining GUID becomes a pseudonym nobody can resolve back to a person.

Free-text fields (issue titles, descriptions, comments, uploaded file names and contents)
can of course contain whatever a user typed into them. They are not treated as identifiers
and are not erased — see §3.

## 2. Credentials and security data

| Data | Where | Notes |
|---|---|---|
| Password hash | `Users.PasswordHash` | ASP.NET Identity hasher; the plaintext is never stored or logged |
| Refresh tokens | `refresh_tokens` | Hard-deleted on erasure |
| Email verification tokens | `email_verification_tokens` | Only the SHA-256 hash of the token is stored; a database read cannot be replayed into an account takeover |
| Security stamp | `Users.SecurityStamp` | Rotating it invalidates every live session |
| Client IP addresses | Gateway rate-limiter partitions (in memory), Serilog request logs in Seq | Not persisted in any application database |

## 3. What self-service deletion does

`POST /api/v1/identity/users/me/delete` (password confirmation required):

**Erased**
- `Users.Email` → `deleted-<userid>@deleted.invalid` (`.invalid` is reserved by RFC 2606
  and can never route to a real mailbox)
- `Users.UserName` → `deleted_<12 hex chars>`
- `Users.PasswordHash` → a sentinel that no password can verify against
- `EmailVerifiedAt`, `PasswordChangedAt`, `LastActiveOrganizationId` cleared

**Hard-deleted**
- refresh tokens, email verification tokens, organization memberships, role assignments
- any invite still addressed to that email address

**Deliberately kept**
- Issues, comments, sprints, audit entries and uploaded files, still carrying the user's
  GUID. They are part of other people's working history: deleting them would silently
  rewrite a shared workspace, and the audit trail exists precisely so changes can be
  accounted for. After erasure the GUID resolves to nothing, so the UI shows a deleted
  user rather than a name.
- The `Users` row itself, soft-deleted and stripped. Keeping the id prevents it from being
  reissued and keeps foreign references from dangling.

The whole operation runs in one transaction, so it cannot half-apply. The security stamp
rotates, so every existing session for that account is refused within seconds.

**Not covered by this flow:** if the departing user was the only member of an organization,
that organization is left with no members. There is no ownership-transfer step yet. The
handler does refuse to erase the last active system administrator, since that would leave
the deployment unadministrable.

## 4. Retention

| Store | Retention | Configured by |
|---|---|---|
| Application databases | Until the account is erased or the record is deleted by a user | — |
| Database dumps | 7 days by default | `BACKUP_RETENTION_DAYS` (`scripts/backup/backup.sh`) |
| Seq logs | **Not configured — set this before launch** | Seq UI → Settings → Retention |

### Backups still contain erased data

This is the honest gap. A dump taken before an erasure request still holds the address, and
overwriting it selectively is not practical. What makes it bounded is the retention window:
with the default `BACKUP_RETENTION_DAYS=7`, an erased address survives in backups for at
most seven days and is then pruned along with the dump. If the retention window is raised,
the erasure guarantee weakens by exactly the same amount.

A restore from a dump older than an erasure request would resurrect the erased row. If that
ever happens, the erasure has to be re-applied against the restored database.

### Logs

Application logs are written with structured properties and deliberately avoid recording
email addresses in the account-erasure path — writing the address into the log store would
just relocate the personal data instead of removing it. Logs elsewhere may still contain
addresses (registration and login failures log identifiers), which is the main reason Seq
needs a retention policy rather than an unbounded disk.

## 5. Where data leaves the system

- **Outbound email** — SMTP relay configured through `SMTP_*`. Verification and invite
  emails carry the recipient's address by definition. In development this is MailHog, which
  stores everything in memory and is never started in production.
- **Nothing else.** There is no analytics, no third-party tracking, and no external API
  that receives user data. The AI feature calls Ollama on the deployment host itself, so
  prompt contents (which can include issue titles and descriptions) never leave the server.
