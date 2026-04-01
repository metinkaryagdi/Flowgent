namespace Shared.Abstractions.Messaging;

/// <summary>
/// Event bus abstraction for publishing and subscribing to events
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event to the message broker
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IIntegrationEvent;
    
    /// <summary>
    /// Publishes raw message (for outbox pattern)
    /// </summary>
    Task PublishRawAsync(string eventType, string payload, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Legacy subscription hook kept only for backward compatibility.
    /// Queue topology is owned by service-specific BackgroundService consumers.
    /// </summary>
    [Obsolete("Legacy generic queue topology is disabled. Declare service-specific queues inside a BackgroundService consumer.")]
    void Subscribe<TEvent, THandler>() 
        where TEvent : IIntegrationEvent
        where THandler : IEventHandler<TEvent>;
}

/// <summary>
/// Handler for processing integration events
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
