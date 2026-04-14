using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IssueService.Infrastructure.Repositories;

public sealed class IssueCommentRepository : IIssueCommentRepository
{
    private readonly IssueDbContext _dbContext;

    public IssueCommentRepository(IssueDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(IssueComment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.IssueComments.AddAsync(comment, cancellationToken);
    }

    public async Task<IReadOnlyList<IssueComment>> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueComments
            .Where(x => x.IssueId == issueId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.IssueComments
            .Where(x => x.IssueId == issueId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
            _dbContext.IssueComments.RemoveRange(items);
    }
}
