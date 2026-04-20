using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.AcceptInviteExisting;

public sealed record AcceptInviteExistingCommand(Guid Token, Guid UserId) : IRequest<UserDto>;
