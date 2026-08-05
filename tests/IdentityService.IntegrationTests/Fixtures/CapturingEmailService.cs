using System.Collections.Concurrent;
using System.Web;
using BitirmeProject.IdentityService.Application.Abstractions;

namespace IdentityService.IntegrationTests.Fixtures;

/// <summary>
/// Stands in for the SMTP-backed <c>EmailService</c> in integration tests.
///
/// Registering this instead of poking the database directly means the tests exercise the
/// real path: the token is generated and hashed by production code, the link is built by
/// production code, and verification goes through the real HTTP endpoint. A test that
/// flipped <c>EmailVerifiedAt</c> in SQL would still pass if token generation were broken.
/// </summary>
public sealed class CapturingEmailService : IEmailService
{
    private readonly ConcurrentQueue<(string Email, string Link)> _verificationEmails = new();
    private readonly ConcurrentQueue<(string Email, string Link)> _inviteEmails = new();

    public Task SendInviteEmailAsync(
        string toEmail,
        string organizationName,
        string inviteLink,
        CancellationToken cancellationToken = default)
    {
        _inviteEmails.Enqueue((toEmail, inviteLink));
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(
        string toEmail,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        _verificationEmails.Enqueue((toEmail, verificationLink));
        return Task.CompletedTask;
    }

    /// <summary>The raw token from the most recent verification email sent to an address.</summary>
    public string? GetLatestVerificationToken(string email)
    {
        var link = _verificationEmails
            .Where(e => string.Equals(e.Email, email, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Link)
            .LastOrDefault();

        if (link is null)
            return null;

        var query = HttpUtility.ParseQueryString(new Uri(link).Query);
        return query["token"];
    }

    public int VerificationEmailCount(string email) =>
        _verificationEmails.Count(e => string.Equals(e.Email, email, StringComparison.OrdinalIgnoreCase));

    public void Clear()
    {
        _verificationEmails.Clear();
        _inviteEmails.Clear();
    }
}
