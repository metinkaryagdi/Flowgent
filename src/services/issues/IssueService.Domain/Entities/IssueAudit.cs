using BitirmeProject.IssueService.Domain.Enums;

namespace BitirmeProject.IssueService.Domain.Entities;

public sealed class IssueAudit
{
    public Guid Id { get; private set; }
    public Guid IssueId { get; private set; }
    public IssueStatus FromStatus { get; private set; }
    public IssueStatus ToStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private IssueAudit() { }

    public IssueAudit(Guid issueId, IssueStatus fromStatus, IssueStatus toStatus, Guid changedByUserId)
    {
        Id = Guid.NewGuid();
        IssueId = issueId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        ChangedAt = DateTime.UtcNow;
    }
}
