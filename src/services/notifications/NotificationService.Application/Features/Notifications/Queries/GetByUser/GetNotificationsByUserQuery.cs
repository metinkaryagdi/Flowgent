using BitirmeProject.NotificationService.Application.DTOs;
using MediatR;

namespace BitirmeProject.NotificationService.Application.Features.Notifications.Queries.GetByUser;

public sealed record GetNotificationsByUserQuery(Guid UserId) : IRequest<IReadOnlyList<NotificationDto>>;
