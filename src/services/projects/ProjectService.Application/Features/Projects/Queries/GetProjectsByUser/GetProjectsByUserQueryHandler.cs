using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUser;

public sealed class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IProjectRepository _repository;
    private readonly IMapper _mapper;

    public GetProjectsByUserQueryHandler(IProjectRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        var projects = await _repository.GetByOwnerUserIdAsync(request.UserId, cancellationToken);
        return projects.Select(project => _mapper.Map<ProjectDto>(project)).ToList();
    }
}
