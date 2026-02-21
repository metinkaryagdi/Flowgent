using System.Text.Json;
using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Domain.Entities;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;

public sealed class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public AddMemberCommandHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<ProjectDto> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project", request.ProjectId);

        if (await _memberRepository.ExistsAsync(request.ProjectId, request.UserId, cancellationToken))
            throw new BusinessRuleException("User is already a member of this project.");

        var member = new ProjectMember(request.ProjectId, request.UserId, request.AddedByUserId);
        await _memberRepository.AddAsync(member, cancellationToken);

        var evt = new MemberAddedEvent(request.ProjectId, request.UserId, request.AddedByUserId, request.CorrelationId ?? Guid.Empty);
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
