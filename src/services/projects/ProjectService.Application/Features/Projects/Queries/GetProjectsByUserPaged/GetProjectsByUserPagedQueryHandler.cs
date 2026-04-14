using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUserPaged;

public sealed class GetProjectsByUserPagedQueryHandler : IRequestHandler<GetProjectsByUserPagedQuery, PagedResult<ProjectDto>>
{
    private readonly IProjectRepository _repository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IMapper _mapper;

    public GetProjectsByUserPagedQueryHandler(IProjectRepository repository, IProjectSummaryRepository summaryRepository, IMapper mapper)
    {
        _repository = repository;
        _summaryRepository = summaryRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProjectDto>> Handle(GetProjectsByUserPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetByMemberUserIdPagedAsync(
            request.UserId,
            request.OrganizationId,
            request.Page,
            request.PageSize,
            request.Search,
            request.IncludeArchived,
            cancellationToken);

        var summaries = await _summaryRepository.GetByProjectIdsAsync(items.Select(x => x.Id).ToArray(), cancellationToken);
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
