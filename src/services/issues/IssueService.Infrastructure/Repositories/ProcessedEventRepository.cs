using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IssueService.Infrastructure.Repositories;

public sealed class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly IssueDbContext _dbContext;

    public ProcessedEventRepository(IssueDbContext dbContext)
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
