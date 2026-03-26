using BitirmeProject.ProjectService.Domain.Enums;

namespace BitirmeProject.ProjectService.Domain.Entities;

public sealed class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AddedByUserId { get; private set; }
    public DateTime AddedAt { get; private set; }
    public ProjectMemberRole Role { get; private set; }

    private ProjectMember() { }

    public ProjectMember(Guid projectId, Guid userId, Guid addedByUserId, ProjectMemberRole role)
    {
        ProjectId = projectId;
        UserId = userId;
        AddedByUserId = addedByUserId;
        AddedAt = DateTime.UtcNow;
        Role = role;
    }
}
