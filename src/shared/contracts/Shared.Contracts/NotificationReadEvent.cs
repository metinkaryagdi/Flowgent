namespace Shared.Contracts.Events;

public sealed record NotificationReadEvent : IntegrationEvent
{
    public Guid NotificationId { get; init; }
    public Guid UserId { get; init; }
    public DateTime ReadAtUtc { get; init; }

    public NotificationReadEvent() { }

    public NotificationReadEvent(
        Guid notificationId,
        Guid userId,
        DateTime readAtUtc,
        Guid correlationId)
        : base(correlationId)
    {
        NotificationId = notificationId;
        UserId = userId;
        ReadAtUtc = readAtUtc;
    }
}
