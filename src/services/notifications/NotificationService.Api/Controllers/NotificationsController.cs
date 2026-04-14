using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;
using BitirmeProject.NotificationService.Application.Features.Notifications.Queries.GetByUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Extensions;

namespace BitirmeProject.NotificationService.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<NotificationDto>> Create([FromBody] CreateNotificationCommand command)
    {
        var requesterId = User.TryGetUserId();
        if (requesterId is null)
            return Unauthorized();

        var safeCommand = User.HasRole("Admin")
            ? command
            : command with { UserId = requesterId.Value };

        var result = await _mediator.Send(safeCommand);
        return Ok(result);
    }

    /// <summary>
    /// Returns notifications for the given user.
    /// Accessible by: the user themselves or an Admin.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetByUser(Guid userId)
    {
        var requesterId = User.TryGetUserId();
        var isAdmin = User.HasRole("Admin");

        if (!isAdmin && requesterId != userId)
            return Forbid();

        var result = await _mediator.Send(new GetNotificationsByUserQuery(userId));
        return Ok(result);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<NotificationDto>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new MarkNotificationReadCommand(id, userId.Value), cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var notifications = await _mediator.Send(new GetNotificationsByUserQuery(userId.Value), cancellationToken);
        var count = notifications.Count(n => !n.IsRead);
        return Ok(new { count });
    }

    [HttpPost("read-all")]
    public async Task<ActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var notifications = await _mediator.Send(new GetNotificationsByUserQuery(userId.Value), cancellationToken);

        foreach (var n in notifications.Where(n => !n.IsRead))
            await _mediator.Send(new MarkNotificationReadCommand(n.Id, userId.Value), cancellationToken);

        return Ok();
    }
}
