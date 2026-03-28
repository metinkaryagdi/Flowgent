using AutoMapper;
using System.Text.Json;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.RemoveIssue;

public sealed class RemoveIssueCommandHandler : IRequestHandler<RemoveIssueCommand, SprintIssueDto>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintIssueRepository _issueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public RemoveIssueCommandHandler(
        ISprintRepository sprintRepository,
        ISprintIssueRepository issueRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _sprintRepository = sprintRepository;
        _issueRepository = issueRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<SprintIssueDto> Handle(RemoveIssueCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            throw new NotFoundException("Sprint", request.SprintId);

        if (sprint.Status == SprintStatus.Completed)
            throw new BusinessRuleException("Cannot remove issues from a completed sprint.");

        var sprintIssue = await _issueRepository.GetByIssueIdAsync(request.IssueId, cancellationToken);
        if (sprintIssue is null)
            throw new NotFoundException("SprintIssue", request.IssueId);

        if (sprintIssue.SprintId != sprint.Id)
            throw new BusinessRuleException("Issue is not assigned to this sprint.");

        sprintIssue.SprintId = null;
        sprintIssue.UpdatedAt = DateTime.UtcNow;
        await _issueRepository.UpdateAsync(sprintIssue, cancellationToken);

        var evt = new IssueRemovedFromSprintEvent(
            sprintIssue.IssueId,
            sprintIssue.ProjectId,
            sprint.Id,
            request.RemovedByUserId,
            request.CorrelationId ?? Guid.Empty);

        var outbox = new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        };

        await _outboxRepository.AddAsync(outbox, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SprintIssueDto>(sprintIssue);
    }
}
