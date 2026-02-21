using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IIssueAuditRepository
{
    Task AddAsync(IssueAudit audit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueAudit>> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
}
