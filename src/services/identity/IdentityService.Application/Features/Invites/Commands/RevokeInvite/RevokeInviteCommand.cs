using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.RevokeInvite;

public sealed record RevokeInviteCommand(
    Guid InviteId,
    Guid RequestedByUserId) : IRequest;
