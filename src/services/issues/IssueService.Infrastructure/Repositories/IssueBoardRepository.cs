using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IssueService.Infrastructure.Repositories;

public sealed class IssueBoardRepository : IIssueBoardRepository
{
    private readonly IssueDbContext _dbContext;

    public IssueBoardRepository(IssueDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IssueBoardItem?> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueBoardItems.FirstOrDefaultAsync(x => x.IssueId == issueId, cancellationToken);
    }

    public async Task<IReadOnlyList<IssueBoardItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueBoardItems
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(IssueBoardItem boardItem, CancellationToken cancellationToken = default)
    {
        await _dbContext.IssueBoardItems.AddAsync(boardItem, cancellationToken);
    }

    public Task UpdateAsync(IssueBoardItem boardItem, CancellationToken cancellationToken = default)
    {
        _dbContext.IssueBoardItems.Update(boardItem);
        return Task.CompletedTask;
    }
}
