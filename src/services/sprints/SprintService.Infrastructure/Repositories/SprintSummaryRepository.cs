using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Domain.Entities;
using BitirmeProject.SprintService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.SprintService.Infrastructure.Repositories;

public sealed class SprintSummaryRepository : ISprintSummaryRepository
{
    private readonly SprintDbContext _dbContext;

    public SprintSummaryRepository(SprintDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SprintSummary?> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SprintSummaries
            .FirstOrDefaultAsync(x => x.SprintId == sprintId, cancellationToken);
    }

    public async Task AddAsync(SprintSummary summary, CancellationToken cancellationToken = default)
    {
        await _dbContext.SprintSummaries.AddAsync(summary, cancellationToken);
    }
}
