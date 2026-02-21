using AutoMapper;
using System.Text.Json;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.AddIssue;

public sealed class AddIssueCommandHandler : IRequestHandler<AddIssueCommand, SprintIssueDto>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintIssueRepository _issueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public AddIssueCommandHandler(
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

    public async Task<SprintIssueDto> Handle(AddIssueCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            throw new NotFoundException("Sprint", request.SprintId);

        if (sprint.Status == SprintStatus.Completed)
            throw new BusinessRuleException("Cannot add issues to a completed sprint.");

        var sprintIssue = await _issueRepository.GetByIssueIdAsync(request.IssueId, cancellationToken);
        if (sprintIssue is null)
            throw new NotFoundException("SprintIssue", request.IssueId);

        if (sprintIssue.ProjectId != sprint.ProjectId)
            throw new BusinessRuleException("Issue belongs to a different project.");

        if (sprintIssue.SprintId.HasValue && sprintIssue.SprintId != sprint.Id)
            throw new BusinessRuleException("Issue already belongs to another sprint.");

        if (sprintIssue.SprintId != sprint.Id)
        {
            sprintIssue.AssignToSprint(sprint.Id);
            await _issueRepository.UpdateAsync(sprintIssue, cancellationToken);

            var evt = new IssueAddedToSprintEvent(
                sprintIssue.IssueId,
                sprintIssue.ProjectId,
                sprint.Id,
                request.AddedByUserId,
                request.CorrelationId ?? Guid.Empty);

            var outbox = new OutboxMessage
            {
                EventType = evt.GetType().Name,
                Payload = JsonSerializer.Serialize(evt),
                OccurredOn = evt.OccurredOn
            };

            await _outboxRepository.AddAsync(outbox, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return _mapper.Map<SprintIssueDto>(sprintIssue);
    }
}
