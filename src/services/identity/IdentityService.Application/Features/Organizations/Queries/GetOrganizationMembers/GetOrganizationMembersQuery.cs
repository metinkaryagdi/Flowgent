using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Queries.GetOrganizationMembers;

public sealed record GetOrganizationMembersQuery(
    Guid OrganizationId,
    Guid RequestedByUserId) : IRequest<IReadOnlyList<OrganizationMemberDto>>;
