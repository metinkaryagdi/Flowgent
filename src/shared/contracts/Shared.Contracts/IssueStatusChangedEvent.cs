namespace Shared.Contracts.Events;

public sealed record IssueStatusChangedEvent : IntegrationEvent
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public Guid ChangedByUserId { get; init; }

    public IssueStatusChangedEvent() { }

    public IssueStatusChangedEvent(Guid issueId, Guid projectId, string oldStatus, string newStatus, Guid changedByUserId, Guid correlationId)
        : base(correlationId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
    }
}