using BitirmeProject.IdentityService.Domain.Entities;

namespace BitirmeProject.IdentityService.Application.Abstractions;

public sealed record JwtTokenResult(string AccessToken, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    JwtTokenResult Generate(User user, IReadOnlyList<string> roles, Guid? organizationId = null, string? orgRole = null);
}