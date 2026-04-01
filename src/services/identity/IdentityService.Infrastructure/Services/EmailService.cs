using BitirmeProject.IdentityService.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace BitirmeProject.IdentityService.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _fromAddress;

    public EmailService(IConfiguration configuration)
    {
        _host = configuration["Email:Host"] ?? "mailhog";
        _port = int.TryParse(configuration["Email:Port"], out var p) ? p : 1025;
        _fromAddress = configuration["Email:From"] ?? "noreply@bitirmeproject.local";
    }

    public async Task SendInviteEmailAsync(
        string toEmail,
        string organizationName,
        string inviteLink,
        CancellationToken cancellationToken = default)
    {
        var subject = $"{organizationName} - Davet";
        var body = $"""
            Merhaba,

            {organizationName} organizasyonuna davet edildiniz.

            Katılmak için aşağıdaki bağlantıya tıklayın:
            {inviteLink}

            Bu davet 48 saat içinde geçerliliğini yitirecektir.
            """;

        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = false,
            Credentials = CredentialCache.DefaultNetworkCredentials
        };

        var message = new MailMessage(_fromAddress, toEmail, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
