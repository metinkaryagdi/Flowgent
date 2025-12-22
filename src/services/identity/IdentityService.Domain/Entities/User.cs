using BitirmeProject.IdentityService.Domain.Common;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;

public class User : BaseEntity
{
    private readonly List<UserRole> _userRoles = new();

    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserStatus Status { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    // EF Core için parameterless ctor
    private User() { }

    public User(string userName, string email, string passwordHash)
    {
        SetUserName(userName);
        SetEmail(email);
        SetPasswordHash(passwordHash);
        Status = UserStatus.Active;
    }

    public bool IsActive => Status == UserStatus.Active;

    public void SetUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("Username cannot be empty.", nameof(userName));

        UserName = userName.Trim();
        MarkUpdated();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        Email = email.Trim();
        MarkUpdated();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        PasswordHash = passwordHash;
        MarkUpdated();
    }

    public void ChangeStatus(UserStatus status)
    {
        Status = status;
        MarkUpdated();
    }

    public void AddRole(Role role)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        if (_userRoles.Any(ur => ur.RoleId == role.Id))
            return;

        _userRoles.Add(new UserRole(Id, role.Id));
        MarkUpdated();
    }

    public void RemoveRole(Guid roleId)
    {
        var link = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (link is null)
            return;

        _userRoles.Remove(link);
        MarkUpdated();
    }

    // Soft delete fonksiyonu ekliyoruz
    public void SoftDelete(Guid deletedBy)
    {
        SoftDelete(deletedBy);
    }
}
