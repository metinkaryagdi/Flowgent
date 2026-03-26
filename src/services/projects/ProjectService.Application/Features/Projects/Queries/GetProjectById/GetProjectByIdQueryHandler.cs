using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectById;

public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    private readonly IProjectRepository _repository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IMapper _mapper;

    public GetProjectByIdQueryHandler(IProjectRepository repository, IProjectSummaryRepository summaryRepository, IMapper mapper)
    {
        _repository = repository;
        _summaryRepository = summaryRepository;
        _mapper = mapper;
    }

    public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (project is null)
            return null;

        var summary = await _summaryRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        return ProjectDtoFactory.Create(project, summary);
    }
}
