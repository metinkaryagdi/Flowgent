namespace BitirmeProject.IdentityService.Domain.Common;

public abstract class BaseEntity : IEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    // Soft delete
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    // 🔥 Soft delete state değişimini domain içinde yap
    public void SoftDelete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
