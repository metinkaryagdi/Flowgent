using Shared.Abstractions.Messaging;

namespace Shared.Abstractions.Domain;

/// <summary>
/// Base class for aggregate roots
/// Aggregates can raise domain events
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IIntegrationEvent> _domainEvents = new();
    
    public IReadOnlyCollection<IIntegrationEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected AggregateRoot() : base() { }
    
    protected AggregateRoot(TId id) : base(id) { }
    
    protected void AddDomainEvent(IIntegrationEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
