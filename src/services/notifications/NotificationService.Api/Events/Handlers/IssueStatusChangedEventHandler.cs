using System.Net.Http.Json;
using BitirmeProject.NotificationService.Api.Models;
using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Api.Events.Handlers;

public sealed class IssueStatusChangedEventHandler : IEventHandler<IssueStatusChangedEvent>
{
    private readonly ILogger<IssueStatusChangedEventHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IHttpClientFactory _httpClientFactory;

    public IssueStatusChangedEventHandler(
        ILogger<IssueStatusChangedEventHandler> logger,
        IMediator mediator,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _mediator = mediator;
        _httpClientFactory = httpClientFactory;
    }

    public async Task HandleAsync(IssueStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var issue = await TryGetIssueAsync(@event.IssueId, cancellationToken);
        if (issue is null)
        {
            _logger.LogWarning("IssueStatusChangedEvent: Issue not found. IssueId={IssueId}", @event.IssueId);
            return;
        }

        var recipient = ResolveRecipient(issue, @event.ChangedByUserId);
        if (recipient == Guid.Empty)
            return;

        var command = new CreateNotificationCommand(
            recipient,
            "Issue status changed",
            $"Issue {@event.IssueId} status changed from {@event.OldStatus} to {@event.NewStatus}.",
            "InApp",
            "Issue",
            @event.IssueId,
            @event.CorrelationId,
            @event.EventId);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "IssueStatusChangedEvent handled. IssueId={IssueId}, RecipientUserId={RecipientUserId}",
            @event.IssueId,
            recipient);
    }

    private static Guid ResolveRecipient(IssueDto issue, Guid actorUserId)
    {
        if (issue.AssigneeUserId.HasValue && issue.AssigneeUserId.Value != actorUserId)
            return issue.AssigneeUserId.Value;

        if (issue.CreatedByUserId != actorUserId)
            return issue.CreatedByUserId;

        return Guid.Empty;
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
