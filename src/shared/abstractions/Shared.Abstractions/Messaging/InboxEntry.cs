namespace Shared.Abstractions.Messaging;

/// <summary>
/// Represents a record of a consumed integration event used to ensure idempotent processing.
/// Stored per consumer (service name) to prevent duplicate handling after crashes.
/// </summary>
public class InboxEntry
{
    /// <summary>The EventId of the consumed integration event.</summary>
    public Guid EventId { get; private set; }

    /// <summary>The name of the consuming service (e.g., "SprintService", "NotificationService").</summary>
    public string ConsumerName { get; private set; } = string.Empty;

    /// <summary>The event type name for debugging purposes.</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>UTC timestamp when the event was processed.</summary>
    public DateTime ProcessedOn { get; private set; }

    private InboxEntry() { }

    public InboxEntry(Guid eventId, string consumerName, string eventType)
    {
        EventId = eventId;
        ConsumerName = consumerName.Trim();
        EventType = eventType.Trim();
        ProcessedOn = DateTime.UtcNow;
    }
}
