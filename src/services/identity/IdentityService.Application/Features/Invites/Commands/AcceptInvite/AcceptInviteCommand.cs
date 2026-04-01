using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.AcceptInvite;

public sealed record AcceptInviteCommand(
    Guid Token,
    string UserName,
    string Password) : IRequest<UserDto>;
