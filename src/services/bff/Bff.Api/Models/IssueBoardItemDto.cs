namespace BitirmeProject.Bff.Api.Models;

public sealed class IssueBoardItemDto
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public int Priority { get; init; }
    public Guid? AssigneeUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int Version { get; init; }
}
