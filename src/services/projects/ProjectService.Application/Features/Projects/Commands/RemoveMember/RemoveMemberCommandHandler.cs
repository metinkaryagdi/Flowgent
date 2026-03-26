using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RemoveMemberCommandHandler(
        IProjectRepository projectRepository,
        IProjectSummaryRepository summaryRepository,
        IProjectMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _summaryRepository = summaryRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProjectDto> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project", request.ProjectId);

        var targetMember = await _memberRepository.GetAsync(request.ProjectId, request.UserId, cancellationToken);
        if (targetMember is null)
            throw new NotFoundException("ProjectMember", request.UserId);

        if (request.UserId == project.OwnerUserId || targetMember.Role == ProjectMemberRole.Owner)
            throw new BusinessRuleException("Project owner cannot be removed from membership.");

        var requester = await _memberRepository.GetAsync(request.ProjectId, request.RemovedByUserId, cancellationToken);
        var requesterRole = request.RemovedByUserId == project.OwnerUserId
            ? ProjectMemberRole.Owner
            : requester?.Role;

        var isSelfRemoval = request.RemovedByUserId == request.UserId;
        if (!isSelfRemoval && (requesterRole is null || requesterRole == ProjectMemberRole.Member))
            throw new BusinessRuleException("Only project owners or admins can remove other members.");

        if (targetMember.Role == ProjectMemberRole.Admin && requesterRole != ProjectMemberRole.Owner && !isSelfRemoval)
            throw new BusinessRuleException("Only the project owner can remove admins.");

        await _memberRepository.RemoveAsync(request.ProjectId, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = await _summaryRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        return ProjectDtoFactory.Create(project, summary);
    }
}
