namespace BitirmeProject.IdentityService.Application.Options;

public sealed class JwtOptions
{
    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
    public int RefreshTokenDays { get; init; } = 7;
}
