namespace Shared.Contracts.Events;

public sealed record UserInvitedEvent : IntegrationEvent
{
    public string Email { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public Guid InvitedByUserId { get; init; }
    public string Role { get; init; } = string.Empty;
    public Guid? InvitedUserId { get; init; }

    public UserInvitedEvent() { }

    public UserInvitedEvent(
        string email,
        Guid organizationId,
        string organizationName,
        Guid invitedByUserId,
        string role,
        Guid? invitedUserId = null)
    {
        Email = email;
        OrganizationId = organizationId;
        OrganizationName = organizationName;
        InvitedByUserId = invitedByUserId;
        Role = role;
        InvitedUserId = invitedUserId;
    }
}
