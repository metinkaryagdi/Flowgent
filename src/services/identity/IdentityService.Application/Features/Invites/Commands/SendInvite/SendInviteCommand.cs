using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.SendInvite;

public sealed record SendInviteCommand(
    Guid OrganizationId,
    Guid InvitedByUserId,
    string Email,
    OrganizationRole Role,
    string InviteLinkBaseUrl) : IRequest<InviteDto>;
