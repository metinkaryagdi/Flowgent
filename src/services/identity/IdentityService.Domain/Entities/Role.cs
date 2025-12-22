using BitirmeProject.IdentityService.Domain.Common;

namespace BitirmeProject.IdentityService.Domain.Entities;

public class Role : BaseEntity
{
    private readonly List<UserRole> _userRoles = new();

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role() { }

    public Role(string name, string? description = null)
    {
        SetName(name);
        Description = description;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty.", nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated();
    }
}
