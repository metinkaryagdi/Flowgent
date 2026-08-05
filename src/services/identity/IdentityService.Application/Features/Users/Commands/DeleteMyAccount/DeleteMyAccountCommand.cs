using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.DeleteMyAccount;

/// <summary>
/// Self-service account erasure. The password is re-checked here even though the caller
/// is already authenticated: a stolen session should not be enough to destroy an account.
/// </summary>
public sealed record DeleteMyAccountCommand(Guid UserId, string Password) : IRequest<Unit>;
