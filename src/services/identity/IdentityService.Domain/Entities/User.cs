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

    /// <summary>Number of consecutive failed login attempts. Reset on successful login.</summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>When set, the account is locked until this UTC time.</summary>
    public DateTime? LockoutEnd { get; private set; }

    /// <summary>Changes whenever security-sensitive state changes (role change, password change, status change).
    /// Can be embedded in JWT and validated on each request to detect stale tokens.</summary>
    public Guid SecurityStamp { get; private set; } = Guid.NewGuid();

    /// <summary>UTC time the password was last changed.</summary>
    public DateTime? PasswordChangedAt { get; private set; }

    /// <summary>The organization this user was most recently active in. Used to restore org context on login/refresh.</summary>
    public Guid? LastActiveOrganizationId { get; private set; }

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

        UserName = userName.Trim().ToLowerInvariant();
        MarkUpdated();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        // Normalize: trim + lowercase for consistent uniqueness checks
        Email = email.Trim().ToLowerInvariant();
        MarkUpdated();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        PasswordHash = passwordHash;
        PasswordChangedAt = DateTime.UtcNow;
        SecurityStamp = Guid.NewGuid(); // Invalidate any existing sessions
        MarkUpdated();
    }

    public void ChangeStatus(UserStatus status)
    {
        Status = status;
        SecurityStamp = Guid.NewGuid(); // Invalidate sessions on status change
        MarkUpdated();
    }

    /// <summary>Records a failed login attempt. Returns true if account should be locked.</summary>
    public bool RecordFailedLogin(int maxAttempts = 5)
    {
        FailedLoginCount++;
        MarkUpdated();
        if (FailedLoginCount >= maxAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(15);
            SecurityStamp = Guid.NewGuid();
            return true;
        }
        return false;
    }

    /// <summary>Resets failed login counter on successful login.</summary>
    public void RecordSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        MarkUpdated();
    }

    /// <summary>True if the account is currently locked out.</summary>
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;

    public void AddRole(Role role)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        if (_userRoles.Any(ur => ur.RoleId == role.Id))
            return;

        _userRoles.Add(new UserRole(Id, role));
        SecurityStamp = Guid.NewGuid(); // Invalidate sessions on role change
        MarkUpdated();
    }

    /// <summary>Records the last organization the user actively switched to. Persisted so login can restore the correct context.</summary>
    public void SetActiveOrganization(Guid? organizationId)
    {
        LastActiveOrganizationId = organizationId;
        MarkUpdated();
    }

    public void RemoveRole(Guid roleId)
    {
        var link = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (link is null)
            return;

        _userRoles.Remove(link);
        SecurityStamp = Guid.NewGuid(); // Invalidate sessions on role change
        MarkUpdated();
    }

    // Soft delete fonksiyonu ekliyoruz
    public void SoftDelete(Guid deletedBy)
    {
        base.SoftDelete();
    }
}
