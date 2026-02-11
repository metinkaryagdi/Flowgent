namespace BitirmeProject.IdentityService.Application.DTOs;

public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}