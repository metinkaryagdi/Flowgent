using System.Text.Json;
using AutoMapper;
using System.Diagnostics;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.Common.Mappings;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;

public sealed class ChangeIssueStatusCommandHandler : IRequestHandler<ChangeIssueStatusCommand, IssueDto>
{
    private readonly IIssueRepository _repository;
    private readonly IIssueAuditRepository _auditRepository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ChangeIssueStatusCommandHandler> _logger;
    private readonly IDistributedCache _cache;

    public ChangeIssueStatusCommandHandler(
        IIssueRepository repository,
        IIssueAuditRepository auditRepository,
        IIssueBoardRepository boardRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper,
        ILogger<ChangeIssueStatusCommandHandler> logger,
        IDistributedCache cache)
    {
        _repository = repository;
        _auditRepository = auditRepository;
        _boardRepository = boardRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IssueDto> Handle(ChangeIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var issue = await _repository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new NotFoundException("Issue", request.IssueId);

        if (issue.Version != request.ExpectedVersion)
            throw new ConcurrencyException("Issue version conflict.");

        var oldStatus = issue.Status;
        issue.ChangeStatus(request.NewStatus);
        if (oldStatus == issue.Status)
        {
            var currentBoardItem = await _boardRepository.GetByIssueIdAsync(issue.Id, cancellationToken);
            return IssueDtoFactory.Create(issue, currentBoardItem?.SprintId);
        }

        var statusEvent = new IssueStatusChangedEvent(issue.Id, issue.ProjectId, oldStatus.ToString(), request.NewStatus.ToString(), request.ChangedByUserId, issue.Title, issue.CreatedByUserId, issue.AssigneeUserId, request.CorrelationId ?? Guid.Empty);
        var statusOutbox = new OutboxMessage
        {
            EventType = statusEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(statusEvent),
            OccurredOn = statusEvent.OccurredOn
        };
        await _outboxRepository.AddAsync(statusOutbox, cancellationToken);

        var audit = new IssueAudit(issue.Id, oldStatus, issue.Status, request.ChangedByUserId);
        await _auditRepository.AddAsync(audit, cancellationToken);

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

        sw.Stop();
        _logger.LogInformation(
            "Issue status change projection updated in {ElapsedMs}ms for IssueId={IssueId}",
            sw.ElapsedMilliseconds,
            issue.Id);

        return IssueDtoFactory.Create(issue, boardItem.SprintId);
    }
}
