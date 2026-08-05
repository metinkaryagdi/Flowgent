using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.VerifyEmail;

/// <summary>Confirms an address using the raw token from the emailed link.</summary>
public sealed record VerifyEmailCommand(string Token) : IRequest<Unit>;
