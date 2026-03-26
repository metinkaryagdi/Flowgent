using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Api.Events.Handlers;

public sealed class IssueCreatedEventHandler : IEventHandler<IssueCreatedEvent>
{
    private readonly IProjectSummaryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueCreatedEventHandler> _logger;

    public IssueCreatedEventHandler(
        IProjectSummaryRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<IssueCreatedEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(IssueCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var summary = await _repository.GetByProjectIdAsync(@event.ProjectId, cancellationToken);
        if (summary is null)
        {
            summary = new ProjectSummary(@event.ProjectId);
            await _repository.AddAsync(summary, cancellationToken);
        }

        summary.RegisterIssueCreated();
        await _repository.UpdateAsync(summary, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "IssueCreatedEvent processed. IssueId={IssueId}, ProjectId={ProjectId}",
            @event.IssueId, @event.ProjectId);
    }
}
