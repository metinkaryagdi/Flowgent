using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.ProjectService.Infrastructure.Repositories;

public sealed class ProjectSummaryRepository : IProjectSummaryRepository
{
    private readonly ProjectDbContext _dbContext;

    public ProjectSummaryRepository(ProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectSummary?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProjectSummaries.FirstOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectSummary>> GetByProjectIdsAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken = default)
    {
        if (projectIds.Count == 0)
            return Array.Empty<ProjectSummary>();

        return await _dbContext.ProjectSummaries
            .Where(x => projectIds.Contains(x.ProjectId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProjectSummary summary, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProjectSummaries.AddAsync(summary, cancellationToken);
    }

    public Task UpdateAsync(ProjectSummary summary, CancellationToken cancellationToken = default)
    {
        _dbContext.ProjectSummaries.Update(summary);
        return Task.CompletedTask;
    }
}
