using System.Text.Json;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;

public sealed class ChangeIssueStatusCommandHandler : IRequestHandler<ChangeIssueStatusCommand, IssueDto>
{
    private readonly IIssueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public ChangeIssueStatusCommandHandler(
        IIssueRepository repository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<IssueDto> Handle(ChangeIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await _repository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new InvalidOperationException("Issue not found.");

        var oldStatus = issue.Status;
        issue.ChangeStatus(request.NewStatus);

        var statusEvent = new IssueStatusChangedEvent(issue.Id, issue.ProjectId, oldStatus.ToString(), request.NewStatus.ToString(), request.ChangedByUserId, request.CorrelationId ?? Guid.Empty);
        var statusOutbox = new OutboxMessage
        {
            EventType = statusEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(statusEvent),
            OccurredOn = statusEvent.OccurredOn
        };
        await _outboxRepository.AddAsync(statusOutbox, cancellationToken);

        var notificationEvent = new NotificationRequestedEvent(
            Guid.NewGuid(),
            request.ChangedByUserId,
            "Issue status changed",
            $"Issue {issue.Id} status changed from {oldStatus} to {request.NewStatus}.",
            "InApp",
            "Issue",
            issue.Id,
            request.CorrelationId ?? Guid.Empty);

        var notificationOutbox = new OutboxMessage
        {
            EventType = notificationEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(notificationEvent),
            OccurredOn = notificationEvent.OccurredOn
        };
        await _outboxRepository.AddAsync(notificationOutbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<IssueDto>(issue);
    }
}
