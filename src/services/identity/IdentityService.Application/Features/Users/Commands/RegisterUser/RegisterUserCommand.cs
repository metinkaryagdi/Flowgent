using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string UserName,
    string Email,
    string Password) : IRequest<UserDto>;
