namespace Shared.Contracts.Events;

public sealed record IssueCreatedEvent : IntegrationEvent
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }

    public IssueCreatedEvent() { }

    public IssueCreatedEvent(Guid issueId, Guid projectId, Guid? organizationId, string title, string issueType, string priority, Guid createdByUserId, Guid correlationId)
        : base(correlationId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        OrganizationId = organizationId;
        Title = title;
        IssueType = issueType;
        Priority = priority;
        CreatedByUserId = createdByUserId;
    }
}