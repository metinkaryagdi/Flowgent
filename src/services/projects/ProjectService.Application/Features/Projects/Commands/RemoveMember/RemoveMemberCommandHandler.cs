using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RemoveMemberCommandHandler(
        IProjectRepository projectRepository,
        IProjectMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProjectDto> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project", request.ProjectId);

        await _memberRepository.RemoveAsync(request.ProjectId, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProjectDto>(project);
    }
}
