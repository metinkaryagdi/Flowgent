using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUser;

public sealed record GetProjectsByUserQuery(Guid UserId, Guid? OrganizationId = null) : IRequest<IReadOnlyList<ProjectDto>>;
