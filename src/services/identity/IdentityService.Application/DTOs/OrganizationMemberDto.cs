namespace BitirmeProject.IdentityService.Application.DTOs;

public sealed class OrganizationMemberDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Role { get; init; } = null!;
    public DateTime JoinedAt { get; init; }
}
