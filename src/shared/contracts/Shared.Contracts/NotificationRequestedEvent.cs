namespace Shared.Contracts.Events;

public sealed record NotificationRequestedEvent : IntegrationEvent
{
    public Guid NotificationId { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }

    public NotificationRequestedEvent() { }

    public NotificationRequestedEvent(
        Guid notificationId,
        Guid userId,
        string title,
        string message,
        string channel,
        string entityType,
        Guid entityId,
        Guid correlationId)
        : base(correlationId)
    {
        NotificationId = notificationId;
        UserId = userId;
        Title = title;
        Message = message;
        Channel = channel;
        EntityType = entityType;
        EntityId = entityId;
    }
}
