namespace BitirmeProject.IdentityService.Application.Abstractions;

public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string organizationName, string inviteLink, CancellationToken cancellationToken = default);

    /// <summary>Sends the single-use link that activates a self-registered account.</summary>
    Task SendEmailVerificationAsync(string toEmail, string verificationLink, CancellationToken cancellationToken = default);
}
