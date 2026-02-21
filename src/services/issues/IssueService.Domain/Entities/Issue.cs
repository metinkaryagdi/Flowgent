using Shared.Abstractions.Domain;
using Shared.Abstractions.Exceptions;
using BitirmeProject.IssueService.Domain.Enums;
using BitirmeProject.IssueService.Domain.Workflow;

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
    public Guid? SprintId { get; private set; }
    public int Version { get; private set; } = 1;

    private Issue() { }

    public Issue(Guid projectId, string title, string? description, IssuePriority priority, Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        SetTitleInternal(title);
        SetDescriptionInternal(description);
        Priority = priority;
        Status = IssueStatus.Open;
        CreatedByUserId = createdByUserId;
        Version = 1;
    }

    public void SetTitle(string title)
    {
        SetTitleInternal(title);
        Touch();
    }

    public void SetDescription(string? description)
    {
        SetDescriptionInternal(description);
        Touch();
    }

    public void AssignTo(Guid assigneeUserId)
    {
        if (AssigneeUserId == assigneeUserId)
            return;

        AssigneeUserId = assigneeUserId;
        Touch();
    }

    public void ChangeStatus(IssueStatus status)
    {
        if (Status == status)
            return;

        if (!IssueWorkflow.IsTransitionAllowed(Status, status))
            throw new BusinessRuleException($"Invalid status transition from {Status} to {status}.");

        Status = status;
        Touch();
    }

    public void ChangePriority(IssuePriority priority)
    {
        if (Priority == priority)
            return;

        Priority = priority;
        Touch();
    }

    public void AssignToSprint(Guid sprintId)
    {
        if (SprintId == sprintId)
            return;

        SprintId = sprintId;
        Touch();
    }

    public void RemoveFromSprint()
    {
        if (!SprintId.HasValue)
            return;

        SprintId = null;
        Touch();
    }

    private void SetTitleInternal(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title.Trim();
    }

    private void SetDescriptionInternal(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
