using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Domain.Entities;
using BitirmeProject.SprintService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.SprintService.Infrastructure.Repositories;

public sealed class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly SprintDbContext _dbContext;

    public ProcessedEventRepository(SprintDbContext dbContext)
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
