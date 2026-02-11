using BitirmeProject.ProjectService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Api.Events.Handlers;

public sealed class IssueStatusChangedEventHandler : IEventHandler<IssueStatusChangedEvent>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueStatusChangedEventHandler> _logger;

    public IssueStatusChangedEventHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<IssueStatusChangedEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(IssueStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(@event.ProjectId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Project not found for IssueStatusChangedEvent. ProjectId={ProjectId}", @event.ProjectId);
            return;
        }

        project.ApplyIssueStatusChange(@event.OldStatus, @event.NewStatus);
        await _repository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "IssueStatusChangedEvent processed. IssueId={IssueId}, ProjectId={ProjectId}, Old={OldStatus}, New={NewStatus}",
            @event.IssueId, @event.ProjectId, @event.OldStatus, @event.NewStatus);
    }
}