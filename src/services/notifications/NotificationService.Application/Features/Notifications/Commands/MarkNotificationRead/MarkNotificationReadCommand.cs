using BitirmeProject.NotificationService.Application.DTOs;
using MediatR;

namespace BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<NotificationDto>;
