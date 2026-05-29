namespace BitirmeProject.AiService.Domain.Entities;

public sealed class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTime ProcessedOn { get; private set; }

    private ProcessedEvent() { }

    public ProcessedEvent(Guid eventId, string eventType)
    {
        EventId = eventId;
        EventType = eventType.Trim();
        ProcessedOn = DateTime.UtcNow;
    }
}
