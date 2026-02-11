using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string UserNameOrEmail,
    string Password) : IRequest<AuthResponseDto>;