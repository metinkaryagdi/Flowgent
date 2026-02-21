using BitirmeProject.SprintService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.SprintService.Api.Events.Handlers;

public sealed class IssueStatusChangedEventHandler : IEventHandler<IssueStatusChangedEvent>
{
    private readonly ILogger<IssueStatusChangedEventHandler> _logger;
    private readonly ISprintIssueRepository _issueRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IssueStatusChangedEventHandler(
        ILogger<IssueStatusChangedEventHandler> logger,
        ISprintIssueRepository issueRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _issueRepository = issueRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(IssueStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var sprintIssue = await _issueRepository.GetByIssueIdAsync(@event.IssueId, cancellationToken);
        if (sprintIssue is null)
        {
            _logger.LogWarning(
                "IssueStatusChangedEvent received but SprintIssue not found. IssueId={IssueId}",
                @event.IssueId);
            return;
        }

        sprintIssue.UpdateStatus(@event.NewStatus);
        await _issueRepository.UpdateAsync(sprintIssue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "IssueStatusChangedEvent projected into SprintIssue. IssueId={IssueId}, OldStatus={OldStatus}, NewStatus={NewStatus}",
            @event.IssueId,
            @event.OldStatus,
            @event.NewStatus);
    }
}
