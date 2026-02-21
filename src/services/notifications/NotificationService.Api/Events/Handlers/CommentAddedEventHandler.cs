using System.Net.Http.Json;
using BitirmeProject.NotificationService.Api.Models;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public CommentAddedEventHandler(
        ILogger<CommentAddedEventHandler> logger,
        IMediator mediator,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _mediator = mediator;
        _httpClientFactory = httpClientFactory;
    }

    public async Task HandleAsync(CommentAddedEvent @event, CancellationToken cancellationToken = default)
    {
        var issue = await TryGetIssueAsync(@event.IssueId, cancellationToken);
        if (issue is null)
        {
            _logger.LogWarning("CommentAddedEvent: Issue not found. IssueId={IssueId}", @event.IssueId);
            return;
        }

        var recipient = issue.AssigneeUserId ?? issue.CreatedByUserId;
        if (recipient == Guid.Empty || recipient == @event.AuthorUserId)
            return;

        var command = new CreateNotificationCommand(
            recipient,
            "New comment",
            $"A new comment was added to issue {@event.IssueId}.",
            "InApp",
            "Issue",
            @event.IssueId,
            @event.CorrelationId,
            @event.EventId);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("CommentAddedEvent handled. IssueId={IssueId}", @event.IssueId);
    }

    private async Task<IssueDto?> TryGetIssueAsync(Guid issueId, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("IssueService");
        var response = await client.GetAsync($"/api/v1/issues/{issueId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<IssueDto>(cancellationToken: cancellationToken);
    }
}
