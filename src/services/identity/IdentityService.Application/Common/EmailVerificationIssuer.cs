using System.Security.Cryptography;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BitirmeProject.IdentityService.Application.Common;

/// <summary>Issues and emails single-use email-verification links.</summary>
public interface IEmailVerificationIssuer
{
    /// <summary>
    /// Retires any outstanding tokens for the user, mints a new one, and emails the link.
    /// The caller owns the transaction: this only stages repository writes, it does not save.
    /// </summary>
    Task IssueAsync(User user, CancellationToken cancellationToken = default);
}

public sealed class EmailVerificationIssuer : IEmailVerificationIssuer
{
    /// <summary>Matches the 24 hours promised in the email body.</summary>
    public const int ExpiryHours = 24;

    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly ILogger<EmailVerificationIssuer> _logger;

    public EmailVerificationIssuer(
        IEmailVerificationTokenRepository tokens,
        IEmailService emailService,
        IOptions<AppOptions> appOptions,
        ILogger<EmailVerificationIssuer> logger)
    {
        _tokens = tokens;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task IssueAsync(User user, CancellationToken cancellationToken = default)
    {
        // Re-issuing retires the previous link, so a forwarded or shoulder-surfed old
        // email stops working as soon as the user asks for a new one.
        var outstanding = await _tokens.GetActiveByUserIdAsync(user.Id, cancellationToken);
        foreach (var previous in outstanding)
        {
            previous.Invalidate();
            await _tokens.UpdateAsync(previous, cancellationToken);
        }

        var rawToken = GenerateRawToken();
        var token = new EmailVerificationToken(
            user.Id,
            user.Email,
            TokenHasher.Hash(rawToken),
            ExpiryHours);

        await _tokens.AddAsync(token, cancellationToken);

        var link = $"{_appOptions.BaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(rawToken)}";

        // A dead SMTP relay must not roll back a registration that already succeeded --
        // the user would be told the address was taken on every retry. Log loudly and let
        // them use the resend endpoint instead.
        try
        {
            await _emailService.SendEmailVerificationAsync(user.Email, link, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send verification email. UserId={UserId}. The token is stored; the user can request a resend.",
                user.Id);
        }
    }

    /// <summary>
    /// URL-safe, 256 bits of entropy. Base64Url avoids the '+' and '/' that would need
    /// escaping in a query string and get mangled by mail clients that re-wrap long lines.
    /// </summary>
    private static string GenerateRawToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
