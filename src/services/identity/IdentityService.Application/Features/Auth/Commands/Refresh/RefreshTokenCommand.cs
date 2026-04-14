using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken, Guid? OrganizationId = null) : IRequest<AuthResponseDto>;
