using BitirmeProject.ProjectService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Api.Events.Handlers;

public sealed class IssueAssignedEventHandler : IEventHandler<IssueAssignedEvent>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueAssignedEventHandler> _logger;

    public IssueAssignedEventHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<IssueAssignedEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(IssueAssignedEvent @event, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(@event.ProjectId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Project not found for IssueAssignedEvent. ProjectId={ProjectId}", @event.ProjectId);
            return;
        }

        project.RegisterIssueAssigned(@event.AssigneeUserId);
        await _repository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "IssueAssignedEvent processed. IssueId={IssueId}, ProjectId={ProjectId}, Assignee={AssigneeUserId}",
            @event.IssueId, @event.ProjectId, @event.AssigneeUserId);
    }
}