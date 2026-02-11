namespace Shared.Contracts.Events;

public sealed record ProjectUpdatedEvent : IntegrationEvent
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public Guid UpdatedByUserId { get; init; }

    public ProjectUpdatedEvent() { }

    public ProjectUpdatedEvent(Guid projectId, string name, string key, Guid updatedByUserId, Guid correlationId)
        : base(correlationId)
    {
        ProjectId = projectId;
        Name = name;
        Key = key;
        UpdatedByUserId = updatedByUserId;
    }
}