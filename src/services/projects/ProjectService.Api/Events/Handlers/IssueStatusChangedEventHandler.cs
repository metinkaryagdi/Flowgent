using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Api.Events.Handlers;

public sealed class IssueStatusChangedEventHandler : IEventHandler<IssueStatusChangedEvent>
{
    private readonly IProjectSummaryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueStatusChangedEventHandler> _logger;

    public IssueStatusChangedEventHandler(
        IProjectSummaryRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<IssueStatusChangedEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(IssueStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var summary = await _repository.GetByProjectIdAsync(@event.ProjectId, cancellationToken);
        if (summary is null)
        {
            summary = new ProjectSummary(@event.ProjectId);
            await _repository.AddAsync(summary, cancellationToken);
        }

        summary.ApplyIssueStatusChange(@event.OldStatus, @event.NewStatus);
        await _repository.UpdateAsync(summary, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "IssueStatusChangedEvent processed. IssueId={IssueId}, ProjectId={ProjectId}, Old={OldStatus}, New={NewStatus}",
            @event.IssueId, @event.ProjectId, @event.OldStatus, @event.NewStatus);
    }
}
