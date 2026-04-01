using System.Text.Json;
using System.Diagnostics;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.Common.Mappings;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;

public sealed class AssignIssueCommandHandler : IRequestHandler<AssignIssueCommand, IssueDto>
{
    private readonly IIssueRepository _repository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AssignIssueCommandHandler> _logger;
    private readonly IDistributedCache _cache;

    public AssignIssueCommandHandler(
        IIssueRepository repository,
        IIssueBoardRepository boardRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper,
        ILogger<AssignIssueCommandHandler> logger,
        IDistributedCache cache)
    {
        _repository = repository;
        _boardRepository = boardRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IssueDto> Handle(AssignIssueCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var issue = await _repository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new NotFoundException("Issue", request.IssueId);

        if (issue.Version != request.ExpectedVersion)
            throw new ConcurrencyException("Issue version conflict.");

        var hadAssignee = issue.AssigneeUserId;
        issue.AssignTo(request.AssigneeUserId);
        if (issue.AssigneeUserId == hadAssignee)
        {
            var currentBoardItem = await _boardRepository.GetByIssueIdAsync(issue.Id, cancellationToken);
            return IssueDtoFactory.Create(issue, currentBoardItem?.SprintId);
        }

        var assignedEvent = new IssueAssignedEvent(issue.Id, issue.ProjectId, request.AssigneeUserId, request.AssignedByUserId, request.CorrelationId ?? Guid.Empty);
        var assignedOutbox = new OutboxMessage
        {
            EventType = assignedEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(assignedEvent),
            OccurredOn = assignedEvent.OccurredOn
        };
        await _outboxRepository.AddAsync(assignedOutbox, cancellationToken);

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

        try { await _cache.RemoveAsync($"board:project:{issue.ProjectId}", cancellationToken); } catch { }

        sw.Stop();
        _logger.LogInformation(
            "Issue assignment projection updated in {ElapsedMs}ms for IssueId={IssueId}",
            sw.ElapsedMilliseconds,
            issue.Id);

        return IssueDtoFactory.Create(issue, boardItem.SprintId);
    }
}
