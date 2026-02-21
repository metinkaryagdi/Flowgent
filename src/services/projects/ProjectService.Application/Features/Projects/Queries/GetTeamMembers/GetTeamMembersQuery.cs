using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetTeamMembers;

public sealed record GetTeamMembersQuery(Guid ProjectId) : IRequest<IReadOnlyList<ProjectMemberDto>>;
