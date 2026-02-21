namespace Shared.Contracts.Events;

public sealed record IssueRemovedFromSprintEvent : IntegrationEvent
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid SprintId { get; init; }
    public Guid RemovedByUserId { get; init; }

    public IssueRemovedFromSprintEvent() { }

    public IssueRemovedFromSprintEvent(
        Guid issueId,
        Guid projectId,
        Guid sprintId,
        Guid removedByUserId,
        Guid correlationId)
        : base(correlationId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        SprintId = sprintId;
        RemovedByUserId = removedByUserId;
    }
}
