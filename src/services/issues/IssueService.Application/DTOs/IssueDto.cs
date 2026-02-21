using BitirmeProject.IssueService.Domain.Enums;

namespace BitirmeProject.IssueService.Application.DTOs;

public sealed class IssueDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IssueStatus Status { get; init; }
    public IssuePriority Priority { get; init; }
    public Guid CreatedByUserId { get; init; }
    public Guid? AssigneeUserId { get; init; }
    public Guid? SprintId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int Version { get; init; }
}
