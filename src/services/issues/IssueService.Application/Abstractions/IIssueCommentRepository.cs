using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IIssueCommentRepository
{
    Task AddAsync(IssueComment comment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueComment>> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task RemoveByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
}
