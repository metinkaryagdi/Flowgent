using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetAllProjectsPaged;

public sealed class GetAllProjectsPagedQueryHandler
    : IRequestHandler<GetAllProjectsPagedQuery, PagedResult<ProjectDto>>
{
    private readonly IProjectRepository _repository;
    private readonly IProjectSummaryRepository _summaryRepository;

    public GetAllProjectsPagedQueryHandler(
        IProjectRepository repository,
        IProjectSummaryRepository summaryRepository)
    {
        _repository = repository;
        _summaryRepository = summaryRepository;
    }

    public async Task<PagedResult<ProjectDto>> Handle(
        GetAllProjectsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetAllPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.IncludeArchived,
            cancellationToken);

        var summaries = await _summaryRepository.GetByProjectIdsAsync(
            items.Select(x => x.Id).ToArray(), cancellationToken);
        var summaryLookup = summaries.ToDictionary(x => x.ProjectId);

        return new PagedResult<ProjectDto>
        {
            Items = items
                .Select(project => ProjectDtoFactory.Create(
                    project,
                    summaryLookup.TryGetValue(project.Id, out var summary) ? summary : null))
                .ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
