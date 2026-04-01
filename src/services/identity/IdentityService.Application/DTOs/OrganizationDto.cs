namespace BitirmeProject.IdentityService.Application.DTOs;

public sealed class OrganizationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Guid CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public int MemberCount { get; init; }
}
