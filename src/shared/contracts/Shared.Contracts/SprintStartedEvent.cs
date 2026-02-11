namespace Shared.Contracts.Events;

public sealed record SprintStartedEvent : IntegrationEvent
{
    public Guid SprintId { get; init; }
    public Guid ProjectId { get; init; }
    public DateTime StartedOn { get; init; }
    public Guid StartedByUserId { get; init; }

    public SprintStartedEvent() { }

    public SprintStartedEvent(Guid sprintId, Guid projectId, DateTime startedOn, Guid startedByUserId, Guid correlationId)
        : base(correlationId)
    {
        SprintId = sprintId;
        ProjectId = projectId;
        StartedOn = startedOn;
        StartedByUserId = startedByUserId;
    }
}
