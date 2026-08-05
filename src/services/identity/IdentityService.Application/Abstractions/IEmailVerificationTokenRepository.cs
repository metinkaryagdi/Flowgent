using BitirmeProject.IdentityService.Domain.Entities;

namespace BitirmeProject.IdentityService.Application.Abstractions;

public interface IEmailVerificationTokenRepository
{
    /// <summary>Looks a token up by its SHA-256 hash. Callers hash the raw value from the
    /// link before calling; the raw token is never stored.</summary>
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Outstanding (unused, unexpired, not soft-deleted) tokens for a user, so
    /// issuing a replacement can retire them.</summary>
    Task<IReadOnlyList<EmailVerificationToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
}
