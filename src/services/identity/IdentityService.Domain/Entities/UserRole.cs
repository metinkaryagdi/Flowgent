namespace BitirmeProject.IdentityService.Domain.Entities;

public class UserRole
{
    // Composite key: UserId + RoleId (EF tarafında konfigüre edeceğiz)
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public User? User { get; private set; }
    public Role? Role { get; private set; }

    private UserRole() { }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public UserRole(Guid userId, Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        UserId = userId;
        RoleId = role.Id;
        Role = role;
    }
}
