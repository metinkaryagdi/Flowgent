using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Queries.GetPendingInvites;

public sealed record GetPendingInvitesQuery(
    Guid OrganizationId,
    Guid RequestedByUserId) : IRequest<IReadOnlyList<InviteDto>>;
