using System.Text.Json;
using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;

public sealed class CompleteSprintCommandHandler : IRequestHandler<CompleteSprintCommand, SprintDto>
{
    private readonly ISprintRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public CompleteSprintCommandHandler(
        ISprintRepository repository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<SprintDto> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _repository.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            throw new NotFoundException("Sprint", request.SprintId);

        sprint.Complete();
        await _repository.UpdateAsync(sprint, cancellationToken);

        var evt = new SprintCompletedEvent(sprint.Id, sprint.ProjectId, sprint.CompletedAt ?? DateTime.UtcNow, request.CompletedByUserId, request.CorrelationId ?? Guid.Empty);
        var outbox = new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        };
        await _outboxRepository.AddAsync(outbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SprintDto>(sprint);
    }
}
