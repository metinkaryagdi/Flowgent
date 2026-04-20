namespace BitirmeProject.ProjectService.Application.DTOs;

public sealed class ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public Guid OwnerUserId { get; init; }
    public Guid? OrganizationId { get; init; }
    public bool IsArchived { get; init; }
    public int IssueCount { get; init; }
    public int OpenIssueCount { get; init; }
    public int InProgressIssueCount { get; init; }
    public int DoneIssueCount { get; init; }
    public DateTime CreatedAt { get; init; }
}