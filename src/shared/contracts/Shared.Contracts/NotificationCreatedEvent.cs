namespace Shared.Contracts.Events;

public sealed record NotificationCreatedEvent : IntegrationEvent
{
    public Guid NotificationId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public Guid? ExternalEventId { get; init; }

    public NotificationCreatedEvent() { }

    public NotificationCreatedEvent(
        Guid notificationId,
        Guid userId,
        string title,
        string message,
        string channel,
        string status,
        string? entityType,
        Guid? entityId,
        Guid? externalEventId,
        Guid correlationId)
        : base(correlationId)
    {
        NotificationId = notificationId;
        UserId = userId;
        Title = title;
        Message = message;
        Channel = channel;
        Status = status;
        EntityType = entityType;
        EntityId = entityId;
        ExternalEventId = externalEventId;
    }
}
