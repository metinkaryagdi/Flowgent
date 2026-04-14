namespace BitirmeProject.IdentityService.Application.DTOs;

public sealed class UserDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? OrgName { get; init; }
}
