namespace Shared.Abstractions.Domain;

/// <summary>
/// Base class for all entities
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    protected Entity() { }
    
    protected Entity(TId id)
    {
        Id = id;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
            
        if (ReferenceEquals(this, other))
            return true;
            
        if (GetType() != other.GetType())
            return false;
            
        return Id.Equals(other.Id);
    }
    
    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }
    
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null)
            return true;
            
        if (a is null || b is null)
            return false;
            
        return a.Equals(b);
    }
    
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b)
    {
        return !(a == b);
    }
}
