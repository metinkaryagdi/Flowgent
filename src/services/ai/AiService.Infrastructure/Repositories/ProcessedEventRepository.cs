using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.AiService.Infrastructure.Repositories;

public sealed class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly AiDbContext _dbContext;

    public ProcessedEventRepository(AiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
        => _dbContext.ProcessedEvents.AnyAsync(x => x.EventId == eventId, cancellationToken);

    public async Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
        => await _dbContext.ProcessedEvents.AddAsync(processedEvent, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
