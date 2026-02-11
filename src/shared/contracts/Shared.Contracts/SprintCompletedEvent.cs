namespace Shared.Contracts.Events;

public sealed record SprintCompletedEvent : IntegrationEvent
{
    public Guid SprintId { get; init; }
    public Guid ProjectId { get; init; }
    public DateTime CompletedOn { get; init; }
    public Guid CompletedByUserId { get; init; }

    public SprintCompletedEvent() { }

    public SprintCompletedEvent(Guid sprintId, Guid projectId, DateTime completedOn, Guid completedByUserId, Guid correlationId)
        : base(correlationId)
    {
        SprintId = sprintId;
        ProjectId = projectId;
        CompletedOn = completedOn;
        CompletedByUserId = completedByUserId;
    }
}
