namespace BitirmeProject.SprintService.Application.DTOs;

public sealed class SprintVelocityDto
{
    public Guid SprintId { get; init; }
    public int TotalIssues { get; init; }
    public int DoneIssues { get; init; }
}
