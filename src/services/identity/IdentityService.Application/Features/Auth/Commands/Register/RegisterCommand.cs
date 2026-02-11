using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string Password) : IRequest<AuthResponseDto>;