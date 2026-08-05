using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Auth.Commands.ResendVerification;

/// <summary>Re-sends the verification link. Always succeeds from the caller's point of
/// view, whether or not the address belongs to an account (see the handler).</summary>
public sealed record ResendVerificationCommand(string Email) : IRequest<Unit>;
