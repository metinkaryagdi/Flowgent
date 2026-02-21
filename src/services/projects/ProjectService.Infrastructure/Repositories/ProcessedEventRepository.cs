using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.ProjectService.Infrastructure.Repositories;

public sealed class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly ProjectDbContext _dbContext;

    public ProcessedEventRepository(ProjectDbContext dbContext)
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
