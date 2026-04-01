using BitirmeProject.IdentityService.Domain.Enums;

namespace BitirmeProject.IdentityService.Domain.Entities;

public class OrganizationMember
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public OrganizationRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public Organization Organization { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private OrganizationMember() { }

    public OrganizationMember(Guid organizationId, Guid userId, OrganizationRole role)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    internal void SetRole(OrganizationRole role)
    {
        Role = role;
    }
}
