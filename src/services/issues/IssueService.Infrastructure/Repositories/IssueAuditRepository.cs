using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IssueService.Infrastructure.Repositories;

public sealed class IssueAuditRepository : IIssueAuditRepository
{
    private readonly IssueDbContext _dbContext;

    public IssueAuditRepository(IssueDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(IssueAudit audit, CancellationToken cancellationToken = default)
    {
        await _dbContext.IssueAudits.AddAsync(audit, cancellationToken);
    }

    public async Task<IReadOnlyList<IssueAudit>> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueAudits
            .Where(x => x.IssueId == issueId)
            .OrderBy(x => x.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
