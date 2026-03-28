namespace Shared.Contracts.Events;

public sealed record UserCreatedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    public UserCreatedEvent() { }

    public UserCreatedEvent(Guid userId, string userName, string email, Guid correlationId)
        : base(correlationId)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
    }
}
