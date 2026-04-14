using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByOrganizationPaged;

public sealed record GetProjectsByOrganizationPagedQuery(
    Guid OrganizationId,
    int Page,
    int PageSize,
    string? Search,
    bool IncludeArchived) : IRequest<PagedResult<ProjectDto>>;
