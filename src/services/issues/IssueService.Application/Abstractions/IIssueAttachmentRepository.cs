using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Abstractions;

public interface IIssueAttachmentRepository
{
    Task AddAsync(IssueAttachment attachment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IssueAttachment>> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid issueId, Guid fileId, CancellationToken cancellationToken = default);
}
