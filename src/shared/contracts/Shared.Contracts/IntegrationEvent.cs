namespace Shared.Contracts.Events;

using Shared.Abstractions.Messaging;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; } = Guid.Empty;

    protected IntegrationEvent() { }

    protected IntegrationEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    protected IntegrationEvent(Guid eventId, DateTime occurredOn, Guid correlationId)
    {
        EventId = eventId;
        OccurredOn = occurredOn;
        CorrelationId = correlationId;
    }
}