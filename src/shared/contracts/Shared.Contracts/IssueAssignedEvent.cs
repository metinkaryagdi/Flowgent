namespace Shared.Contracts.Events;

public sealed record IssueAssignedEvent : IntegrationEvent
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid AssigneeUserId { get; init; }
    public Guid AssignedByUserId { get; init; }

    public IssueAssignedEvent() { }

    public IssueAssignedEvent(Guid issueId, Guid projectId, Guid assigneeUserId, Guid assignedByUserId, Guid correlationId)
        : base(correlationId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        AssigneeUserId = assigneeUserId;
        AssignedByUserId = assignedByUserId;
    }
}