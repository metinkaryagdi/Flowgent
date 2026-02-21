namespace BitirmeProject.SprintService.Application.DTOs;

public sealed class SprintIssueDto
{
    public Guid IssueId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? SprintId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
