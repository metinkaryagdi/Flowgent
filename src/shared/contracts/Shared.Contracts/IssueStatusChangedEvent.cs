namespace Shared.Contracts.Events;

public sealed record IssueStatusChangedEvent : IntegrationEvent
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public Guid ChangedByUserId { get; init; }
    public string IssueTitle { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }
    public Guid? AssigneeUserId { get; init; }

    public IssueStatusChangedEvent() { }

    public IssueStatusChangedEvent(Guid issueId, Guid projectId, string oldStatus, string newStatus, Guid changedByUserId, string issueTitle, Guid createdByUserId, Guid? assigneeUserId, Guid correlationId)
        : base(correlationId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        IssueTitle = issueTitle;
        CreatedByUserId = createdByUserId;
        AssigneeUserId = assigneeUserId;
    }
}