using Shared.Abstractions.Domain;
using BitirmeProject.NotificationService.Domain.Enums;

namespace BitirmeProject.NotificationService.Domain.Entities;

public sealed class Notification : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public int DeliveryAttemptCount { get; private set; }
    public DateTime? LastDeliveryAttemptAt { get; private set; }
    public DateTime? NextDeliveryAttemptAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? LastFailureReason { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public Guid? ExternalEventId { get; private set; }

    private Notification() { }

    public Notification(
        Guid userId,
        string title,
        string message,
        NotificationChannel channel,
        string? entityType,
        Guid? entityId,
        Guid? externalEventId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SetTitle(title);
        SetMessage(message);
        Channel = channel;
        Status = NotificationStatus.Queued;
        IsRead = false;
        EntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim();
        EntityId = entityId;
        ExternalEventId = externalEventId;
        NextDeliveryAttemptAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSent()
    {
        LastFailureReason = null;
        NextDeliveryAttemptAt = null;
        Status = NotificationStatus.Sent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDelivered()
    {
        LastFailureReason = null;
        NextDeliveryAttemptAt = null;
        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegisterDeliveryAttempt()
    {
        DeliveryAttemptCount += 1;
        LastDeliveryAttemptAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? error, TimeSpan retryDelay)
    {
        LastFailureReason = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        NextDeliveryAttemptAt = DateTime.UtcNow.Add(retryDelay);
        Status = NotificationStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title.Trim();
    }

    private void SetMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        Message = message.Trim();
    }
}
