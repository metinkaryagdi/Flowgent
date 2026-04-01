using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(
    Guid OrganizationId,
    Guid RequestedByUserId,
    Guid TargetUserId,
    OrganizationRole NewRole) : IRequest;
