namespace BitirmeProject.IdentityService.Application.Abstractions;

public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string organizationName, string inviteLink, CancellationToken cancellationToken = default);
}
