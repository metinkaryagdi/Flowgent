using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetAllProjectsPaged;

public sealed record GetAllProjectsPagedQuery(
    int Page,
    int PageSize,
    string? Search,
    bool IncludeArchived) : IRequest<PagedResult<ProjectDto>>;
