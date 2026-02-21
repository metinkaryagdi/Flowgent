using BitirmeProject.NotificationService.Application.DTOs;
using MediatR;

namespace BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public sealed record CreateNotificationCommand(
    Guid UserId,
    string Title,
    string Message,
    string Channel,
    string? EntityType,
    Guid? EntityId,
    Guid? CorrelationId,
    Guid? ExternalEventId) : IRequest<NotificationDto>;
