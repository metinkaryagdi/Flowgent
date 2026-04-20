using AutoMapper;
using System.Text.Json;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.ReadModels;
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
    private readonly IIssueServiceClient _issueServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public AddIssueCommandHandler(
        ISprintRepository sprintRepository,
        ISprintIssueRepository issueRepository,
        IIssueServiceClient issueServiceClient,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _sprintRepository = sprintRepository;
        _issueRepository = issueRepository;
        _issueServiceClient = issueServiceClient;
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
        {
            // Race condition guard: IssueCreatedEvent may not have been processed yet.
            // Verify via IssueService and create projection on-the-fly.
            var metadata = await _issueServiceClient.GetIssueAsync(request.IssueId, request.BearerToken, cancellationToken);
            if (metadata is null)
                throw new NotFoundException("Issue", request.IssueId);

            sprintIssue = new SprintIssue(
                metadata.Id,
                metadata.ProjectId,
                sprint.OrganizationId,
                metadata.Title,
                "Task",
                metadata.Priority,
                metadata.Status,
                metadata.CreatedByUserId);
            await _issueRepository.AddAsync(sprintIssue, cancellationToken);
        }

        if (sprintIssue.ProjectId != sprint.ProjectId)
            throw new BusinessRuleException("Issue belongs to a different project.");

        if (sprintIssue.SprintId.HasValue && sprintIssue.SprintId != sprint.Id)
            throw new BusinessRuleException("Issue already belongs to another sprint.");

        if (sprintIssue.SprintId != sprint.Id)
        {
            sprintIssue.SprintId = sprint.Id;
            sprintIssue.UpdatedAt = DateTime.UtcNow;
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
