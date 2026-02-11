using Shared.Abstractions.Domain;
using BitirmeProject.IssueService.Domain.Enums;

namespace BitirmeProject.IssueService.Domain.Entities;

public sealed class Issue : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public IssueStatus Status { get; private set; }
    public IssuePriority Priority { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? AssigneeUserId { get; private set; }

    private Issue() { }

    public Issue(Guid projectId, string title, string? description, IssuePriority priority, Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        SetTitle(title);
        SetDescription(description);
        Priority = priority;
        Status = IssueStatus.Open;
        CreatedByUserId = createdByUserId;
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTo(Guid assigneeUserId)
    {
        AssigneeUserId = assigneeUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(IssueStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePriority(IssuePriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }
}
