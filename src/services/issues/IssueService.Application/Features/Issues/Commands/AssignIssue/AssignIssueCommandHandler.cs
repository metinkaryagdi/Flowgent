using System.Text.Json;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;

public sealed class AssignIssueCommandHandler : IRequestHandler<AssignIssueCommand, IssueDto>
{
    private readonly IIssueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public AssignIssueCommandHandler(
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

    public async Task<IssueDto> Handle(AssignIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _repository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new InvalidOperationException("Issue not found.");

        issue.AssignTo(request.AssigneeUserId);

        var assignedEvent = new IssueAssignedEvent(issue.Id, issue.ProjectId, request.AssigneeUserId, request.AssignedByUserId, request.CorrelationId ?? Guid.Empty);
        var assignedOutbox = new OutboxMessage
        {
            EventType = assignedEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(assignedEvent),
            OccurredOn = assignedEvent.OccurredOn
        };
        await _outboxRepository.AddAsync(assignedOutbox, cancellationToken);

        var notificationEvent = new NotificationRequestedEvent(
            Guid.NewGuid(),
            request.AssigneeUserId,
            "Issue assigned",
            $"You were assigned to issue {issue.Id}.",
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
