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
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;

public sealed class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, IssueDto>
{
    private readonly IIssueRepository _repository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIssueCommandHandler> _logger;
    private readonly IDistributedCache _cache;

    public CreateIssueCommandHandler(IIssueRepository repository, IIssueBoardRepository boardRepository, IUnitOfWork unitOfWork, IOutboxRepository outboxRepository, IMapper mapper, ILogger<CreateIssueCommandHandler> logger, IDistributedCache cache)
    {
        _repository = repository;
        _boardRepository = boardRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IssueDto> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var issue = new Issue(request.ProjectId, request.Title, request.Description, request.Priority, request.CreatedByUserId, request.OrganizationId);
        await _repository.AddAsync(issue, cancellationToken);

        var boardItem = new IssueBoardItem(issue);
        await _boardRepository.AddAsync(boardItem, cancellationToken);

        var evt = new IssueCreatedEvent(issue.Id, issue.ProjectId, issue.Title, "Task", issue.Priority.ToString(), issue.CreatedByUserId, request.CorrelationId ?? Guid.Empty);
        var outbox = new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        };
        await _outboxRepository.AddAsync(outbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try { await _cache.RemoveAsync($"board:project:{issue.ProjectId}", cancellationToken); } catch { }

        sw.Stop();
        _logger.LogInformation(
            "Issue create projection updated in {ElapsedMs}ms for IssueId={IssueId}",
            sw.ElapsedMilliseconds,
            issue.Id);

        return IssueDtoFactory.Create(issue, boardItem.SprintId);
    }
}
