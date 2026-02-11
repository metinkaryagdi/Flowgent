using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.AssignRoleToUser;

public sealed record AssignRoleToUserCommand(
    Guid UserId,
    Guid RoleId) : IRequest<UserDto>;
