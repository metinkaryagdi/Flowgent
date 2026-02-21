namespace BitirmeProject.SprintService.Domain.Entities;

public sealed class SprintIssue
{
    public Guid IssueId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? SprintId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string IssueType { get; private set; } = string.Empty;
    public string Priority { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private SprintIssue() { }

    public SprintIssue(
        Guid issueId,
        Guid projectId,
        string title,
        string issueType,
        string priority,
        string status,
        Guid createdByUserId)
    {
        IssueId = issueId;
        ProjectId = projectId;
        Title = title.Trim();
        IssueType = issueType.Trim();
        Priority = priority.Trim();
        Status = status.Trim();
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return;

        Status = status.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToSprint(Guid sprintId)
    {
        SprintId = sprintId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveFromSprint()
    {
        SprintId = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
