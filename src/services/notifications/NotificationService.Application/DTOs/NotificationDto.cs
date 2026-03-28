using BitirmeProject.NotificationService.Domain.Enums;

namespace BitirmeProject.NotificationService.Application.DTOs;

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationChannel Channel { get; init; }
    public NotificationStatus Status { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public int DeliveryAttemptCount { get; init; }
    public DateTime? LastDeliveryAttemptAt { get; init; }
    public DateTime? NextDeliveryAttemptAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public string? LastFailureReason { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
