namespace Shared.Contracts.Events;

public sealed record CommentAddedEvent : IntegrationEvent
{
    public Guid CommentId { get; init; }
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid AuthorUserId { get; init; }
    public string IssueTitle { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }
    public Guid? AssigneeUserId { get; init; }

    public CommentAddedEvent() { }

    public CommentAddedEvent(Guid commentId, Guid issueId, Guid projectId, Guid authorUserId, string issueTitle, Guid createdByUserId, Guid? assigneeUserId, Guid correlationId)
        : base(correlationId)
    {
        CommentId = commentId;
        IssueId = issueId;
        ProjectId = projectId;
        AuthorUserId = authorUserId;
        IssueTitle = issueTitle;
        CreatedByUserId = createdByUserId;
        AssigneeUserId = assigneeUserId;
    }
}