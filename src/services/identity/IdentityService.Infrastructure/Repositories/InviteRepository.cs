using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IdentityService.Infrastructure.Repositories;

public sealed class InviteRepository : IInviteRepository
{
    private readonly IdentityDbContext _dbContext;

    public InviteRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InviteToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InviteTokens
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<InviteToken?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InviteTokens
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task<InviteToken?> GetPendingByEmailAndOrganizationAsync(
        string email,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbContext.InviteTokens
            .Where(i => i.Email == normalizedEmail
                        && i.OrganizationId == organizationId
                        && !i.IsUsed
                        && !i.IsDeleted
                        && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InviteToken>> GetPendingByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InviteTokens
            .Where(i => i.OrganizationId == organizationId
                        && !i.IsUsed
                        && !i.IsDeleted
                        && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPendingInviteAsync(
        string email,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InviteTokens
            .AnyAsync(i => i.Email == email
                           && i.OrganizationId == organizationId
                           && !i.IsUsed
                           && !i.IsDeleted
                           && i.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task AddAsync(InviteToken invite, CancellationToken cancellationToken = default)
    {
        await _dbContext.InviteTokens.AddAsync(invite, cancellationToken);
    }

    public Task UpdateAsync(InviteToken invite, CancellationToken cancellationToken = default)
    {
        _dbContext.InviteTokens.Update(invite);
        return Task.CompletedTask;
    }
}
