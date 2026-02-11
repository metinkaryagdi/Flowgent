namespace Shared.Contracts.Events;

public sealed record SagaCompensationRequestedEvent : IntegrationEvent
{
    public string SagaName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }

    public SagaCompensationRequestedEvent() { }

    public SagaCompensationRequestedEvent(
        string sagaName,
        string reason,
        string entityType,
        Guid entityId,
        Guid correlationId)
        : base(correlationId)
    {
        SagaName = sagaName;
        Reason = reason;
        EntityType = entityType;
        EntityId = entityId;
    }
}
