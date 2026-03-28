using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.ReadModels;
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

    public async Task<IReadOnlyList<IssueBoardItem>> GetByIssueIdsAsync(IReadOnlyCollection<Guid> issueIds, CancellationToken cancellationToken = default)
    {
        if (issueIds.Count == 0)
            return Array.Empty<IssueBoardItem>();

        return await _dbContext.IssueBoardItems
            .Where(x => issueIds.Contains(x.IssueId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IssueBoardItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueBoardItems
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IssueBoardItem>> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueBoardItems
            .Where(x => x.SprintId == sprintId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<IssueBoardItem> Items, int TotalCount)> GetByProjectIdPagedAsync(
        Guid projectId,
        int page,
        int pageSize,
        Guid? sprintId = null,
        bool backlogOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IssueBoardItems.Where(x => x.ProjectId == projectId);

        if (backlogOnly)
        {
            query = query.Where(x => x.SprintId == null);
        }
        else if (sprintId.HasValue)
        {
            query = query.Where(x => x.SprintId == sprintId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Status)
            .ThenBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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
