namespace Shared.Abstractions.Messaging;

/// <summary>
/// Base interface for all integration events that are published across services
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredOn { get; }
    
    /// <summary>
    /// Correlation ID for tracking related events across services
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Version number for schema evolution of the event payload
    /// </summary>
    int EventVersion { get; }
}
