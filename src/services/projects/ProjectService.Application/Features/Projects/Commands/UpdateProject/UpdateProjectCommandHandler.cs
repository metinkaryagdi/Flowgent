using System.Text.Json;
using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public UpdateProjectCommandHandler(
        IProjectRepository repository,
        IProjectSummaryRepository summaryRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _repository = repository;
        _summaryRepository = summaryRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project", request.Id);

        var incomingKey = request.Key.Trim().ToUpperInvariant();
        if (!string.Equals(project.Key, incomingKey, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repository.ExistsByKeyAsync(request.Key, cancellationToken))
                throw new BusinessRuleException("Project key already exists.");
        }

        project.SetName(request.Name);
        project.SetKey(request.Key);

        await _repository.UpdateAsync(project, cancellationToken);

        var evt = new ProjectUpdatedEvent(project.Id, project.Name, project.Key, request.UpdatedByUserId, request.CorrelationId ?? Guid.Empty);
        var outbox = new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        };
        await _outboxRepository.AddAsync(outbox, cancellationToken);

        var settingsEvent = new ProjectSettingsUpdatedEvent(project.Id, project.Name, project.Key, request.UpdatedByUserId, request.CorrelationId ?? Guid.Empty);
        var settingsOutbox = new OutboxMessage
        {
            EventType = settingsEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(settingsEvent),
            OccurredOn = settingsEvent.OccurredOn
        };
        await _outboxRepository.AddAsync(settingsOutbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = await _summaryRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        return ProjectDtoFactory.Create(project, summary);
    }
}
