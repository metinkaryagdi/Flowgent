using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.Revoke;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest;
