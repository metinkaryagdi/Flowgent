using BitirmeProject.IssueService.Domain.Enums;

namespace BitirmeProject.IssueService.Domain.Entities;

public sealed class IssueBoardItem
{
    public Guid IssueId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public IssueStatus Status { get; private set; }
    public IssuePriority Priority { get; private set; }
    public Guid? AssigneeUserId { get; private set; }
    public Guid? SprintId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public int Version { get; private set; }

    private IssueBoardItem() { }

    public IssueBoardItem(Issue issue)
    {
        IssueId = issue.Id;
        ProjectId = issue.ProjectId;
        Title = issue.Title;
        Status = issue.Status;
        Priority = issue.Priority;
        AssigneeUserId = issue.AssigneeUserId;
        SprintId = issue.SprintId;
        CreatedAt = issue.CreatedAt;
        UpdatedAt = issue.UpdatedAt;
        Version = issue.Version;
    }

    public void ApplyFrom(Issue issue)
    {
        Title = issue.Title;
        Status = issue.Status;
        Priority = issue.Priority;
        AssigneeUserId = issue.AssigneeUserId;
        SprintId = issue.SprintId;
        UpdatedAt = issue.UpdatedAt ?? DateTime.UtcNow;
        Version = issue.Version;
    }
}
