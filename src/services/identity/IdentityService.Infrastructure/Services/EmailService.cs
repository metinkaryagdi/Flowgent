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
    private readonly bool _enableSsl;
    private readonly string? _username;
    private readonly string? _password;

    public EmailService(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? "mailhog";
        _port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 1025;
        _fromAddress = configuration["Smtp:FromAddress"] ?? "noreply@bitirmeproject.local";
        // MailHog accepts anonymous plaintext; real providers (SendGrid, SES, ...)
        // require STARTTLS plus credentials, so both are configurable.
        _enableSsl = bool.TryParse(configuration["Smtp:EnableSsl"], out var ssl) && ssl;
        _username = configuration["Smtp:Username"];
        _password = configuration["Smtp:Password"];
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

        using var client = new SmtpClient(_host, _port) { EnableSsl = _enableSsl };

        if (!string.IsNullOrWhiteSpace(_username))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_username, _password);
        }
        else
        {
            client.Credentials = CredentialCache.DefaultNetworkCredentials;
        }

        var message = new MailMessage(_fromAddress, toEmail, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(
        string toEmail,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        const string subject = "Flowgent - E-posta adresinizi doğrulayın";
        var body = $"""
            Merhaba,

            Flowgent hesabınızı oluşturmak için bu adresi kullandınız. Hesabınızı
            etkinleştirmek ve giriş yapabilmek için aşağıdaki bağlantıya tıklayın:
            {verificationLink}

            Bu bağlantı 24 saat içinde geçerliliğini yitirecektir.

            Bu hesabı siz oluşturmadıysanız bu e-postayı yok sayabilirsiniz;
            doğrulanmayan hesaplar giriş yapamaz.
            """;

        await SendAsync(toEmail, subject, body, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(_host, _port) { EnableSsl = _enableSsl };

        if (!string.IsNullOrWhiteSpace(_username))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_username, _password);
        }
        else
        {
            client.Credentials = CredentialCache.DefaultNetworkCredentials;
        }

        var message = new MailMessage(_fromAddress, toEmail, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
