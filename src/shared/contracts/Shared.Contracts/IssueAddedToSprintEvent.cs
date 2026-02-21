namespace Shared.Contracts.Events;

public sealed record IssueAddedToSprintEvent : IntegrationEvent
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid SprintId { get; init; }
    public Guid AddedByUserId { get; init; }

    public IssueAddedToSprintEvent() { }

    public IssueAddedToSprintEvent(
        Guid issueId,
        Guid projectId,
        Guid sprintId,
        Guid addedByUserId,
        Guid correlationId)
        : base(correlationId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        SprintId = sprintId;
        AddedByUserId = addedByUserId;
    }
}
