using System.Text.Json;
using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.ReadModels;
using BitirmeProject.SprintService.Domain.Entities;
using BitirmeProject.SprintService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;

public sealed class CompleteSprintCommandHandler : IRequestHandler<CompleteSprintCommand, SprintDto>
{
    private readonly ISprintRepository _repository;
    private readonly ISprintIssueRepository _issueRepository;
    private readonly ISprintSummaryRepository _summaryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public CompleteSprintCommandHandler(
        ISprintRepository repository,
        ISprintIssueRepository issueRepository,
        ISprintSummaryRepository summaryRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _repository = repository;
        _issueRepository = issueRepository;
        _summaryRepository = summaryRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<SprintDto> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _repository.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            throw new NotFoundException("Sprint", request.SprintId);

        var sprintIssues = await _issueRepository.GetBySprintIdAsync(sprint.Id, cancellationToken);
        var incompleteIssues = sprintIssues
            .Where(issue => !string.Equals(issue.Status, "Done", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        switch (request.CarryOverPolicy)
        {
            case SprintCarryOverPolicy.Manual when incompleteIssues.Length > 0:
                throw new BusinessRuleException("Sprint has incomplete issues. Choose backlog or next-sprint carry-over before completing.");

            case SprintCarryOverPolicy.Backlog:
                await MoveIssuesToBacklogAsync(incompleteIssues, sprint, request, cancellationToken);
                break;

            case SprintCarryOverPolicy.NextSprint:
                await MoveIssuesToNextSprintAsync(incompleteIssues, sprint, request, cancellationToken);
                break;
        }

        // Snapshot counts from the pre-carry-over state (sprintIssues captured above)
        var totalIssues = sprintIssues.Count;
        var completedIssues = sprintIssues.Count(i => string.Equals(i.Status, "Done", StringComparison.OrdinalIgnoreCase));

        sprint.Complete();
        await _repository.UpdateAsync(sprint, cancellationToken);

        var summary = new SprintSummary(sprint.Id, totalIssues, completedIssues, sprint.CompletedAt ?? DateTime.UtcNow);
        await _summaryRepository.AddAsync(summary, cancellationToken);

        var evt = new SprintCompletedEvent(sprint.Id, sprint.ProjectId, sprint.CompletedAt ?? DateTime.UtcNow, request.CompletedByUserId, request.CorrelationId ?? Guid.Empty);
        await _outboxRepository.AddAsync(CreateOutboxMessage(evt), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SprintDto>(sprint);
    }

    private async Task MoveIssuesToBacklogAsync(
        IReadOnlyCollection<SprintIssue> issues,
        Sprint sprint,
        CompleteSprintCommand request,
        CancellationToken cancellationToken)
    {
        foreach (var issue in issues)
        {
            issue.SprintId = null;
            issue.UpdatedAt = DateTime.UtcNow;
            await _issueRepository.UpdateAsync(issue, cancellationToken);

            var removedEvent = new IssueRemovedFromSprintEvent(
                issue.IssueId,
                issue.ProjectId,
                sprint.Id,
                request.CompletedByUserId,
                request.CorrelationId ?? Guid.Empty);

            await _outboxRepository.AddAsync(CreateOutboxMessage(removedEvent), cancellationToken);
        }
    }

    private async Task MoveIssuesToNextSprintAsync(
        IReadOnlyCollection<SprintIssue> issues,
        Sprint sprint,
        CompleteSprintCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.NextSprintId.HasValue)
            throw new BusinessRuleException("Next sprint must be provided when carry-over policy is NextSprint.");

        var nextSprint = await _repository.GetByIdAsync(request.NextSprintId.Value, cancellationToken);
        if (nextSprint is null)
            throw new NotFoundException("Sprint", request.NextSprintId.Value);

        if (nextSprint.Id == sprint.Id)
            throw new BusinessRuleException("Carry-over target sprint must be different from the sprint being completed.");

        if (nextSprint.ProjectId != sprint.ProjectId)
            throw new BusinessRuleException("Carry-over target sprint belongs to a different project.");

        if (nextSprint.Status == SprintStatus.Completed)
            throw new BusinessRuleException("Cannot carry issues into a completed sprint.");

        foreach (var issue in issues)
        {
            issue.SprintId = nextSprint.Id;
            issue.UpdatedAt = DateTime.UtcNow;
            await _issueRepository.UpdateAsync(issue, cancellationToken);

            var removedEvent = new IssueRemovedFromSprintEvent(
                issue.IssueId,
                issue.ProjectId,
                sprint.Id,
                request.CompletedByUserId,
                request.CorrelationId ?? Guid.Empty);

            var addedEvent = new IssueAddedToSprintEvent(
                issue.IssueId,
                issue.ProjectId,
                nextSprint.Id,
                request.CompletedByUserId,
                request.CorrelationId ?? Guid.Empty);

            await _outboxRepository.AddAsync(CreateOutboxMessage(removedEvent), cancellationToken);
            await _outboxRepository.AddAsync(CreateOutboxMessage(addedEvent), cancellationToken);
        }
    }

    private static OutboxMessage CreateOutboxMessage(IIntegrationEvent @event)
    {
        return new OutboxMessage
        {
            EventType = @event.GetType().Name,
            Payload = JsonSerializer.Serialize(@event),
            OccurredOn = @event.OccurredOn
        };
    }
}
