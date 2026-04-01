namespace BitirmeProject.IdentityService.Application.DTOs;

public sealed class InviteDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string Role { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
