using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Api.Events.Handlers;

public sealed class IssueRemovedFromSprintEventHandler : IEventHandler<IssueRemovedFromSprintEvent>
{
    private readonly IIssueRepository _issueRepository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueRemovedFromSprintEventHandler> _logger;

    public IssueRemovedFromSprintEventHandler(
        IIssueRepository issueRepository,
        IIssueBoardRepository boardRepository,
        IUnitOfWork unitOfWork,
        ILogger<IssueRemovedFromSprintEventHandler> logger)
    {
        _issueRepository = issueRepository;
        _boardRepository = boardRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(IssueRemovedFromSprintEvent @event, CancellationToken cancellationToken = default)
    {
        var issue = await _issueRepository.GetByIdAsync(@event.IssueId, cancellationToken);
        if (issue is null)
        {
            _logger.LogWarning("IssueRemovedFromSprintEvent ignored. Issue not found. IssueId={IssueId}", @event.IssueId);
            return;
        }

        if (issue.ProjectId != @event.ProjectId)
            throw new BusinessRuleException("Issue project mismatch for sprint removal.");

        issue.RemoveFromSprint();
        await _issueRepository.UpdateAsync(issue, cancellationToken);

        var boardItem = await _boardRepository.GetByIssueIdAsync(issue.Id, cancellationToken);
        if (boardItem is null)
        {
            boardItem = new IssueBoardItem(issue);
            await _boardRepository.AddAsync(boardItem, cancellationToken);
        }
        else
        {
            boardItem.ApplyFrom(issue);
            await _boardRepository.UpdateAsync(boardItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
