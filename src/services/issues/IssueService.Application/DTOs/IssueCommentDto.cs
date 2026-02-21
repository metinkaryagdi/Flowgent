namespace BitirmeProject.IssueService.Application.DTOs;

public sealed class IssueCommentDto
{
    public Guid Id { get; init; }
    public Guid IssueId { get; init; }
    public Guid AuthorUserId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
