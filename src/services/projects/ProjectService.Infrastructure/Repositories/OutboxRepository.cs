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
        return await _dbContext.OutboxMessages
            .Where(x => x.Status == OutboxStatus.Pending)
            .OrderBy(x => x.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (message is null) return;

        message.Status = OutboxStatus.Published;
        message.ProcessedOn = DateTime.UtcNow;
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (message is null) return;

        message.Status = OutboxStatus.Failed;
        message.Error = error;
        message.RetryCount += 1;
        message.ProcessedOn = DateTime.UtcNow;
    }
}