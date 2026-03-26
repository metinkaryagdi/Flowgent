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
        var result = await _mediator.Send(command);
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

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<NotificationDto>> MarkRead(Guid id)
    {
        var userId = User.TryGetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new MarkNotificationReadCommand(id, userId.Value));
        return Ok(result);
    }
}
