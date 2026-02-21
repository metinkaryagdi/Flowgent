namespace BitirmeProject.ProjectService.Application.DTOs;

public sealed class ProjectMemberDto
{
    public Guid ProjectId { get; init; }
    public Guid UserId { get; init; }
    public Guid AddedByUserId { get; init; }
    public DateTime AddedAt { get; init; }
}
