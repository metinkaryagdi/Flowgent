using BitirmeProject.IssueService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Api.Events.Handlers;

public sealed class IssueAddedToSprintEventHandler : IEventHandler<IssueAddedToSprintEvent>
{
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueAddedToSprintEventHandler> _logger;

    public IssueAddedToSprintEventHandler(
        IIssueBoardRepository boardRepository,
        IUnitOfWork unitOfWork,
        ILogger<IssueAddedToSprintEventHandler> logger)
    {
        _boardRepository = boardRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(IssueAddedToSprintEvent @event, CancellationToken cancellationToken = default)
    {
        var boardItem = await _boardRepository.GetByIssueIdAsync(@event.IssueId, cancellationToken);
        if (boardItem is null)
        {
            _logger.LogWarning("IssueAddedToSprintEvent ignored. Board projection not found. IssueId={IssueId}", @event.IssueId);
            return;
        }

        if (boardItem.ProjectId != @event.ProjectId)
            throw new BusinessRuleException("Issue project mismatch for sprint assignment.");

        boardItem.AssignToSprint(@event.SprintId);
        await _boardRepository.UpdateAsync(boardItem, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
