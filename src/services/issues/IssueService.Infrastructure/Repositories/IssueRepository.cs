using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IssueService.Infrastructure.Repositories;

public sealed class IssueRepository : IIssueRepository
{
    private readonly IssueDbContext _dbContext;

    public IssueRepository(IssueDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Issue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Issues.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Issue>> GetByAssigneeAsync(Guid assigneeUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Issues
            .Where(i => i.AssigneeUserId == assigneeUserId)
            .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issue>> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Issues
            .Where(i => i.SprintId == sprintId)
            .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        await _dbContext.Issues.AddAsync(issue, cancellationToken);
    }

    public Task UpdateAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        _dbContext.Issues.Update(issue);
        return Task.CompletedTask;
    }
}
