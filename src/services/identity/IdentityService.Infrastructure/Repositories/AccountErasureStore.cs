using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IdentityService.Infrastructure.Repositories;

public sealed class AccountErasureStore : IAccountErasureStore
{
    private readonly IdentityDbContext _dbContext;

    public AccountErasureStore(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> PurgeAccountArtifactsAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        // ExecuteDeleteAsync issues one DELETE per table instead of loading every row
        // into memory first. It writes immediately rather than waiting for SaveChanges,
        // so the caller must already be inside a transaction for this to be atomic with
        // the anonymisation of the user row itself.
        var removed = 0;

        // IgnoreQueryFilters is not optional here. The context applies a global
        // IsDeleted == false filter to every BaseEntity, and ExecuteDelete honours it --
        // so without this, rows that were already soft-deleted would survive an erasure
        // request. A soft-deleted invite still holds the raw address in its Email column,
        // which is exactly the data this is supposed to remove.
        removed += await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        removed += await _dbContext.EmailVerificationTokens
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        // Matched by address, not by user id: an invite is addressed to an email before
        // any account exists, so it is the one place a stranger's address can linger.
        removed += await _dbContext.InviteTokens
            .IgnoreQueryFilters()
            .Where(x => x.Email == email)
            .ExecuteDeleteAsync(cancellationToken);

        removed += await _dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        removed += await _dbContext.UserRoles
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return removed;
    }
}
