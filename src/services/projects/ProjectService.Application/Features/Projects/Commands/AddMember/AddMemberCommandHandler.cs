using System.Text.Json;
using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;

public sealed class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public AddMemberCommandHandler(
        IProjectRepository projectRepository,
        IProjectSummaryRepository summaryRepository,
        IProjectMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _summaryRepository = summaryRepository;
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

        var requester = await _memberRepository.GetAsync(request.ProjectId, request.AddedByUserId, cancellationToken);
        var requesterRole = request.AddedByUserId == project.OwnerUserId
            ? ProjectMemberRole.Owner
            : requester?.Role;

        if (requesterRole is null || requesterRole == ProjectMemberRole.Member)
            throw new BusinessRuleException("Only project owners or admins can add members.");

        if (await _memberRepository.ExistsAsync(request.ProjectId, request.UserId, cancellationToken))
            throw new BusinessRuleException("User is already a member of this project.");

        if (request.Role == ProjectMemberRole.Owner)
            throw new BusinessRuleException("Owner role can only be assigned during project creation or ownership transfer.");

        var member = new ProjectMember(request.ProjectId, request.UserId, request.AddedByUserId, request.Role);
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

        var summary = await _summaryRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        return ProjectDtoFactory.Create(project, summary);
    }
}
