using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.ReadModels;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.SprintService.Api.Events.Handlers;

public sealed class IssueCreatedEventHandler : IEventHandler<IssueCreatedEvent>
{
    private readonly ILogger<IssueCreatedEventHandler> _logger;
    private readonly ISprintIssueRepository _issueRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IssueCreatedEventHandler(
        ILogger<IssueCreatedEventHandler> logger,
        ISprintIssueRepository issueRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _issueRepository = issueRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(IssueCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var existing = await _issueRepository.GetByIssueIdAsync(@event.IssueId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "IssueCreatedEvent ignored because SprintIssue already exists. IssueId={IssueId}",
                @event.IssueId);
            return;
        }

        var sprintIssue = new SprintIssue(
            @event.IssueId,
            @event.ProjectId,
            @event.Title,
            @event.IssueType,
            @event.Priority,
            "Open",
            @event.CreatedByUserId);

        await _issueRepository.AddAsync(sprintIssue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "IssueCreatedEvent projected into SprintIssue. IssueId={IssueId}, ProjectId={ProjectId}",
            @event.IssueId,
            @event.ProjectId);
    }
}
