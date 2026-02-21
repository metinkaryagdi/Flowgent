using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.NotificationService.Infrastructure.Repositories;

public sealed class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly NotificationDbContext _dbContext;

    public ProcessedEventRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessedEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);
    }

    public async Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProcessedEvents.AddAsync(processedEvent, cancellationToken);
    }
}
