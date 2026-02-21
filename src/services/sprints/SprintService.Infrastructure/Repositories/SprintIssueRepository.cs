using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Domain.Entities;
using BitirmeProject.SprintService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.SprintService.Infrastructure.Repositories;

public sealed class SprintIssueRepository : ISprintIssueRepository
{
    private readonly SprintDbContext _dbContext;

    public SprintIssueRepository(SprintDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SprintIssue?> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SprintIssues.FirstOrDefaultAsync(x => x.IssueId == issueId, cancellationToken);
    }

    public async Task<IReadOnlyList<SprintIssue>> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SprintIssues
            .Where(x => x.SprintId == sprintId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SprintIssue>> GetBacklogByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SprintIssues
            .Where(x => x.ProjectId == projectId && x.SprintId == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SprintIssue sprintIssue, CancellationToken cancellationToken = default)
    {
        await _dbContext.SprintIssues.AddAsync(sprintIssue, cancellationToken);
    }

    public Task UpdateAsync(SprintIssue sprintIssue, CancellationToken cancellationToken = default)
    {
        _dbContext.SprintIssues.Update(sprintIssue);
        return Task.CompletedTask;
    }
}
