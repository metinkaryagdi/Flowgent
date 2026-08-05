namespace BitirmeProject.IdentityService.Application.Common;

/// <summary>
/// Thrown when a login attempt is refused because the account is temporarily locked
/// after too many consecutive failures.
///
/// This is a distinct type rather than a plain <see cref="InvalidOperationException"/>
/// so the API layer can map it to 401 without matching on message text, and so clients
/// can tell "wrong password" apart from "locked out" via a stable error code instead of
/// parsing a human-readable string.
/// </summary>
public sealed class AccountLockedException : Exception
{
    /// <summary>Stable, machine-readable code returned to clients.</summary>
    public const string Code = "account_locked";

    /// <summary>UTC time the lockout expires, when known.</summary>
    public DateTime? LockoutEnd { get; }

    public AccountLockedException(string message, DateTime? lockoutEnd = null)
        : base(message)
    {
        LockoutEnd = lockoutEnd;
    }
}
