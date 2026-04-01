using BitirmeProject.IdentityService.Domain.Common;
using BitirmeProject.IdentityService.Domain.Enums;

namespace BitirmeProject.IdentityService.Domain.Entities;

public class InviteToken : BaseEntity
{
    public Guid Token { get; private set; }
    public string Email { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public OrganizationRole Role { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public Organization Organization { get; private set; } = null!;

    private InviteToken() { }

    public InviteToken(string email, Guid organizationId, Guid invitedByUserId, OrganizationRole role, int expiresInHours = 48)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (role == OrganizationRole.Owner)
            throw new InvalidOperationException("Cannot invite a user as Owner.");

        Token = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        OrganizationId = organizationId;
        InvitedByUserId = invitedByUserId;
        Role = role;
        ExpiresAt = DateTime.UtcNow.AddHours(expiresInHours);
        IsUsed = false;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsValid => !IsUsed && !IsExpired && !IsDeleted;

    public void MarkAsUsed()
    {
        if (!IsValid)
            throw new InvalidOperationException("Invite token is no longer valid.");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Revoke()
    {
        if (IsUsed)
            throw new InvalidOperationException("Cannot revoke an already used invite.");

        SoftDelete();
    }
}
