using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

public sealed class CommentAddedEventHandler : IEventHandler<CommentAddedEvent>
{
    private readonly ILogger<CommentAddedEventHandler> _logger;
    private readonly IMediator _mediator;

    public CommentAddedEventHandler(
        ILogger<CommentAddedEventHandler> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task HandleAsync(CommentAddedEvent @event, CancellationToken cancellationToken = default)
    {
        Guid recipient;
        if (@event.AssigneeUserId.HasValue && @event.AssigneeUserId.Value != Guid.Empty)
        {
            if (@event.AssigneeUserId.Value == @event.AuthorUserId)
                return;
            recipient = @event.AssigneeUserId.Value;
        }
        else if (@event.CreatedByUserId != Guid.Empty
                 && @event.CreatedByUserId != @event.AuthorUserId)
        {
            recipient = @event.CreatedByUserId;
        }
        else
        {
            return;
        }

        var command = new CreateNotificationCommand(
            recipient,
            "New comment",
            $"A new comment was added to issue \"{@event.IssueTitle}\".",
            "InApp",
            "Issue",
            @event.IssueId,
            @event.CorrelationId,
            @event.EventId);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("CommentAddedEvent handled. IssueId={IssueId}", @event.IssueId);
    }
}
