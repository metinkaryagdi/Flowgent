using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;

namespace BitirmeProject.IssueService.Application.ReadModels;

// Read-side projection of an Issue used for board/list queries.
// This is NOT a domain entity. Maintained by command handlers and event handlers.
public sealed class IssueBoardItem
{
    public Guid IssueId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public IssueStatus Status { get; set; }
    public IssuePriority Priority { get; set; }
    public Guid? AssigneeUserId { get; set; }
    public Guid? SprintId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int Version { get; set; }

    private IssueBoardItem() { }

    public IssueBoardItem(Issue issue)
    {
        IssueId = issue.Id;
        ProjectId = issue.ProjectId;
        OrganizationId = issue.OrganizationId;
        Title = issue.Title;
        Status = issue.Status;
        Priority = issue.Priority;
        AssigneeUserId = issue.AssigneeUserId;
        CreatedAt = issue.CreatedAt;
        UpdatedAt = issue.UpdatedAt;
        Version = issue.Version;
    }
}
