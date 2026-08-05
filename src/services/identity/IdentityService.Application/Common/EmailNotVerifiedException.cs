namespace BitirmeProject.IdentityService.Application.Common;

/// <summary>
/// Thrown when a login attempt is refused because the account's email address has never
/// been confirmed.
///
/// Mirrors <see cref="AccountLockedException"/>: a distinct type so the API layer maps it
/// to 401 without matching on message text, and a stable machine-readable code so the web
/// client can offer "resend the link" instead of showing a generic credentials error.
/// </summary>
public sealed class EmailNotVerifiedException : Exception
{
    /// <summary>Stable, machine-readable code returned to clients.</summary>
    public const string Code = "email_not_verified";

    /// <summary>The address the verification link was sent to, so the client can show it.</summary>
    public string Email { get; }

    public EmailNotVerifiedException(string message, string email)
        : base(message)
    {
        Email = email;
    }
}
