using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUser;

public sealed class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IProjectRepository _repository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IMapper _mapper;

    public GetProjectsByUserQueryHandler(IProjectRepository repository, IProjectSummaryRepository summaryRepository, IMapper mapper)
    {
        _repository = repository;
        _summaryRepository = summaryRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        var projects = await _repository.GetByOwnerUserIdAsync(request.UserId, cancellationToken);
        var summaries = await _summaryRepository.GetByProjectIdsAsync(projects.Select(x => x.Id).ToArray(), cancellationToken);
        var summaryLookup = summaries.ToDictionary(x => x.ProjectId);

        return projects
            .Select(project => ProjectDtoFactory.Create(
                project,
                summaryLookup.TryGetValue(project.Id, out var summary) ? summary : null))
            .ToList();
    }
}
