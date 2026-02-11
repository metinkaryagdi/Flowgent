using System.Text.Json;
using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public UpdateProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (project is null)
            throw new InvalidOperationException("Project not found.");

        var incomingKey = request.Key.Trim().ToUpperInvariant();
        if (!string.Equals(project.Key, incomingKey, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repository.ExistsByKeyAsync(request.Key, cancellationToken))
                throw new InvalidOperationException("Project key already exists.");
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProjectDto>(project);
    }
}
