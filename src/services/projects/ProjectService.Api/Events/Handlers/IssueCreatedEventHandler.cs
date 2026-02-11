using BitirmeProject.ProjectService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Api.Events.Handlers;

public sealed class IssueCreatedEventHandler : IEventHandler<IssueCreatedEvent>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueCreatedEventHandler> _logger;

    public IssueCreatedEventHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<IssueCreatedEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(IssueCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(@event.ProjectId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Project not found for IssueCreatedEvent. ProjectId={ProjectId}", @event.ProjectId);
            return;
        }

        project.RegisterIssueCreated();
        await _repository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "IssueCreatedEvent processed. IssueId={IssueId}, ProjectId={ProjectId}",
            @event.IssueId, @event.ProjectId);
    }
}