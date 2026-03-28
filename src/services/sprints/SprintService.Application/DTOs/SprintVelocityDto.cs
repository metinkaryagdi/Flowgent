namespace BitirmeProject.SprintService.Application.DTOs;

public sealed class SprintVelocityDto
{
    public Guid SprintId { get; init; }
    public int TotalIssues { get; init; }
    public int DoneIssues { get; init; }
    /// <summary>True when data comes from the immutable velocity snapshot (completed sprint).</summary>
    public bool IsSnapshot { get; init; }
    /// <summary>UTC timestamp when the snapshot was captured. Null for live calculations.</summary>
    public DateTime? SnapshotTakenAt { get; init; }
}
