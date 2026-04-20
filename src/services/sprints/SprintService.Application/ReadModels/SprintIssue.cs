namespace BitirmeProject.SprintService.Application.ReadModels;

// Read-side projection of an Issue within the Sprint bounded context.
// This is NOT a domain entity. Maintained by event handlers and command handlers.
public sealed class SprintIssue
{
    public Guid IssueId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? SprintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    private SprintIssue() { }

    public SprintIssue(
        Guid issueId,
        Guid projectId,
        Guid? organizationId,
        string title,
        string issueType,
        string priority,
        string status,
        Guid createdByUserId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        OrganizationId = organizationId;
        Title = title.Trim();
        IssueType = issueType.Trim();
        Priority = priority.Trim();
        Status = status.Trim();
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }
}
