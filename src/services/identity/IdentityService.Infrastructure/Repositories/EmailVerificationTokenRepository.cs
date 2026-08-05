using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IdentityService.Infrastructure.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public EmailVerificationTokenRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        // No IsUsed/ExpiresAt filter here on purpose: the handler needs to tell an
        // exhausted token apart from an unknown one for logging, while still returning the
        // same message to the caller.
        return await _dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailVerificationToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailVerificationTokens
            .Where(t => t.UserId == userId
                        && !t.IsUsed
                        && !t.IsDeleted
                        && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.EmailVerificationTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.EmailVerificationTokens.Update(token);
        return Task.CompletedTask;
    }
}
