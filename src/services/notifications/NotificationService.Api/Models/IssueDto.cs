namespace BitirmeProject.NotificationService.Api.Models;

public sealed class IssueDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public Guid? AssigneeUserId { get; init; }
}
