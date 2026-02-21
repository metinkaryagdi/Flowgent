namespace BitirmeProject.Bff.Api.Models;

public sealed class ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public bool IsArchived { get; init; }
    public int IssueCount { get; init; }
    public int OpenIssueCount { get; init; }
    public int InProgressIssueCount { get; init; }
    public int DoneIssueCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
