namespace Shared.Contracts.Events;

public sealed record ProjectCreatedEvent : IntegrationEvent
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public Guid OwnerUserId { get; init; }

    public ProjectCreatedEvent() { }

    public ProjectCreatedEvent(Guid projectId, string name, string key, Guid ownerUserId, Guid correlationId)
        : base(correlationId)
    {
        ProjectId = projectId;
        Name = name;
        Key = key;
        OwnerUserId = ownerUserId;
    }
}