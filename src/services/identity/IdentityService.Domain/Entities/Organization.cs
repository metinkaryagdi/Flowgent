using BitirmeProject.IdentityService.Domain.Common;
using BitirmeProject.IdentityService.Domain.Enums;

namespace BitirmeProject.IdentityService.Domain.Entities;

public class Organization : BaseEntity
{
    private readonly List<OrganizationMember> _members = new();

    public string Name { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyCollection<OrganizationMember> Members => _members.AsReadOnly();

    private Organization() { }

    public Organization(string name, Guid createdByUserId)
    {
        SetName(name);
        CreatedByUserId = createdByUserId;
        _members.Add(new OrganizationMember(Id, createdByUserId, OrganizationRole.Owner));
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name cannot be empty.", nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void AddMember(Guid userId, OrganizationRole role)
    {
        if (role == OrganizationRole.Owner)
            throw new InvalidOperationException("Cannot assign Owner role via AddMember.");

        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this organization.");

        _members.Add(new OrganizationMember(Id, userId, role));
        MarkUpdated();
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User is not a member of this organization.");

        if (member.Role == OrganizationRole.Owner)
            throw new InvalidOperationException("Cannot remove the Owner from the organization.");

        _members.Remove(member);
        MarkUpdated();
    }

    public void ChangeMemberRole(Guid userId, OrganizationRole newRole)
    {
        if (newRole == OrganizationRole.Owner)
            throw new InvalidOperationException("Cannot assign Owner role.");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User is not a member of this organization.");

        if (member.Role == OrganizationRole.Owner)
            throw new InvalidOperationException("Cannot change the Owner's role.");

        member.SetRole(newRole);
        MarkUpdated();
    }

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public OrganizationRole? GetMemberRole(Guid userId) =>
        _members.FirstOrDefault(m => m.UserId == userId)?.Role;
}
