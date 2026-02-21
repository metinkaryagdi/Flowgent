using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetTeamMembers;

public sealed class GetTeamMembersQueryHandler : IRequestHandler<GetTeamMembersQuery, IReadOnlyList<ProjectMemberDto>>
{
    private readonly IProjectMemberRepository _repository;
    private readonly IMapper _mapper;

    public GetTeamMembersQueryHandler(IProjectMemberRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return items.Select(item => _mapper.Map<ProjectMemberDto>(item)).ToList();
    }
}
