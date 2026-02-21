namespace Shared.Contracts.Events;

public sealed record MemberAddedEvent : IntegrationEvent
{
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }
    public Guid AddedByUserId { get; init; }

    public MemberAddedEvent() { }

    public MemberAddedEvent(Guid projectId, Guid userId, Guid addedByUserId, Guid correlationId)
        : base(correlationId)
    {
        ProjectId = projectId;
        UserId = userId;
        AddedByUserId = addedByUserId;
    }
}
