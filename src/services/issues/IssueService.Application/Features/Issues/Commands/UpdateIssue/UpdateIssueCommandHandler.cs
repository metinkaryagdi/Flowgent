using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.Common.Mappings;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.ReadModels;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.UpdateIssue;

public sealed class UpdateIssueCommandHandler : IRequestHandler<UpdateIssueCommand, IssueDto>
{
    private readonly IIssueRepository _repository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;

    public UpdateIssueCommandHandler(
        IIssueRepository repository,
        IIssueBoardRepository boardRepository,
        IUnitOfWork unitOfWork,
        IDistributedCache cache)
    {
        _repository = repository;
        _boardRepository = boardRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<IssueDto> Handle(UpdateIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _repository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new NotFoundException("Issue", request.IssueId);

        if (issue.Version != request.ExpectedVersion)
            throw new ConcurrencyException("Issue version conflict.");

        if (!string.IsNullOrWhiteSpace(request.Title))
            issue.SetTitle(request.Title);

        issue.SetDescription(request.Description);

        if (request.Priority.HasValue)
            issue.ChangePriority(request.Priority.Value);

        var boardItem = await _boardRepository.GetByIssueIdAsync(issue.Id, cancellationToken);
        if (boardItem is null)
        {
            boardItem = new IssueBoardItem(issue);
            await _boardRepository.AddAsync(boardItem, cancellationToken);
        }
        else
        {
            boardItem.Title = issue.Title;
            boardItem.Status = issue.Status;
            boardItem.Priority = issue.Priority;
            boardItem.AssigneeUserId = issue.AssigneeUserId;
            boardItem.UpdatedAt = issue.UpdatedAt ?? DateTime.UtcNow;
            boardItem.Version = issue.Version;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try { await _cache.RemoveAsync($"board:project:{issue.ProjectId}:{issue.OrganizationId}", cancellationToken); } catch { }

        return IssueDtoFactory.Create(issue, boardItem.SprintId);
    }
}
