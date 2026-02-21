namespace BitirmeProject.IssueService.Domain.Entities;

public sealed class IssueComment
{
    public Guid Id { get; private set; }
    public Guid IssueId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private IssueComment() { }

    public IssueComment(Guid issueId, Guid authorUserId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment cannot be empty.", nameof(content));

        Id = Guid.NewGuid();
        IssueId = issueId;
        AuthorUserId = authorUserId;
        Content = content.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
