using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Api.Events.Handlers;

public sealed class IssueAssignedEventHandler : IEventHandler<IssueAssignedEvent>
{
    private readonly ILogger<IssueAssignedEventHandler> _logger;

    public IssueAssignedEventHandler(
        ILogger<IssueAssignedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(IssueAssignedEvent @event, CancellationToken cancellationToken = default)
    {
        // No project aggregate state changes are required for assignment events at the moment.
        // Keep handler for observability and future expansion.
        await Task.CompletedTask;

        _logger.LogInformation(
            "IssueAssignedEvent processed. IssueId={IssueId}, ProjectId={ProjectId}, Assignee={AssigneeUserId}",
            @event.IssueId, @event.ProjectId, @event.AssigneeUserId);
    }
}
