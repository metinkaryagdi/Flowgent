using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUserPaged;

public sealed record GetProjectsByUserPagedQuery(
    Guid UserId,
    Guid? OrganizationId,
    int Page,
    int PageSize,
    string? Search,
    bool IncludeArchived) : IRequest<PagedResult<ProjectDto>>;
