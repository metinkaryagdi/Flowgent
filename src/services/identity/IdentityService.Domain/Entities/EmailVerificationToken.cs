using BitirmeProject.IdentityService.Domain.Common;

namespace BitirmeProject.IdentityService.Domain.Entities;

/// <summary>
/// A single-use token emailed to a self-registered user to prove they control the address.
///
/// Deliberately shaped like <see cref="InviteToken"/> so the two flows stay recognisable,
/// with one important difference: the raw token is NOT stored. Only its SHA-256 hash is
/// persisted, the same way refresh tokens are handled, so a leaked database dump cannot be
/// replayed to activate accounts.
/// </summary>
public class EmailVerificationToken : BaseEntity
{
    /// <summary>SHA-256 of the raw token. The raw value exists only in the email that was sent.</summary>
    public string TokenHash { get; private set; } = null!;

    public Guid UserId { get; private set; }

    /// <summary>The address this token was sent to, captured at issue time so a later
    /// email change cannot be confirmed by an older token.</summary>
    public string Email { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public User User { get; private set; } = null!;

    private EmailVerificationToken() { }

    public EmailVerificationToken(Guid userId, string email, string tokenHash, int expiresInHours = 24)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));

        UserId = userId;
        Email = email.Trim().ToLowerInvariant();
        TokenHash = tokenHash;
        ExpiresAt = DateTime.UtcNow.AddHours(expiresInHours);
        IsUsed = false;
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsValid => !IsUsed && !IsExpired && !IsDeleted;

    public void MarkAsUsed()
    {
        if (!IsValid)
            throw new InvalidOperationException("Email verification token is no longer valid.");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Invalidates an outstanding token, so issuing a new one retires the old link.</summary>
    public void Invalidate() => SoftDelete();
}
