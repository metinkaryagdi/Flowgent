namespace BitirmeProject.ProjectService.Domain.Entities;

public sealed class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AddedByUserId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private ProjectMember() { }

    public ProjectMember(Guid projectId, Guid userId, Guid addedByUserId)
    {
        ProjectId = projectId;
        UserId = userId;
        AddedByUserId = addedByUserId;
        AddedAt = DateTime.UtcNow;
    }
}
