using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;
using BitirmeProject.ProjectService.Infrastructure.Persistence;

namespace BitirmeProject.ProjectService.Infrastructure.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly ProjectDbContext _dbContext;

    public OutboxRepository(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.OutboxMessages
            .Where(x => x.Status == OutboxStatus.Pending
                        && (x.NextRetryAt == null || x.NextRetryAt <= now))
            .OrderBy(x => x.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OutboxMessage>> ClaimBatchAsync(
        Guid lockId, int batchSize, TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var claimUntil = now.Add(lockDuration);

        var candidateIds = await _dbContext.OutboxMessages
            .Where(x => x.Status == OutboxStatus.Pending
                        && (x.NextRetryAt == null || x.NextRetryAt <= now)
                        && (x.ClaimedUntil == null || x.ClaimedUntil <= now))
            .OrderBy(x => x.OccurredOn)
            .Take(batchSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
            return [];

        await _dbContext.OutboxMessages
            .Where(x => candidateIds.Contains(x.Id)
                        && x.Status == OutboxStatus.Pending
                        && (x.ClaimedUntil == null || x.ClaimedUntil <= now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, OutboxStatus.Processing)
                .SetProperty(x => x.LockId, lockId)
                .SetProperty(x => x.ClaimedUntil, claimUntil)
                .SetProperty(x => x.LastAttemptedAt, now),
                cancellationToken);

        return await _dbContext.OutboxMessages
            .Where(x => candidateIds.Contains(x.Id)
                        && x.LockId == lockId
                        && x.Status == OutboxStatus.Processing)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsPublishedAsync(Guid messageId, DateTime publishedOn, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (message is null) return;

        message.Status = OutboxStatus.Published;
        message.PublishedOn = publishedOn;
        message.LockId = null;
        message.ClaimedUntil = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, DateTime? nextRetryAt = null,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (message is null) return;

        message.Status = nextRetryAt.HasValue ? OutboxStatus.Pending : OutboxStatus.Failed;
        message.LastError = error;
        message.RetryCount += 1;
        message.NextRetryAt = nextRetryAt;
        message.LockId = null;
        message.ClaimedUntil = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseOrphanClaimsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var orphans = await _dbContext.OutboxMessages
            .Where(x => x.Status == OutboxStatus.Processing && x.ClaimedUntil <= now)
            .ToListAsync(cancellationToken);

        foreach (var msg in orphans)
        {
            msg.Status = OutboxStatus.Pending;
            msg.LockId = null;
            msg.ClaimedUntil = null;
        }

        if (orphans.Count > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);
    }
}