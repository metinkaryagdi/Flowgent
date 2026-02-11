using System.Text.Json;
using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Domain.Entities;
using MediatR;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public CreateProjectCommandHandler(
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

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByKeyAsync(request.Key, cancellationToken))
            throw new InvalidOperationException("Project key already exists.");

        var project = new Project(request.Name, request.Key, request.OwnerUserId);
        await _repository.AddAsync(project, cancellationToken);

        var evt = new ProjectCreatedEvent(project.Id, project.Name, project.Key, project.OwnerUserId, request.CorrelationId ?? Guid.Empty);
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