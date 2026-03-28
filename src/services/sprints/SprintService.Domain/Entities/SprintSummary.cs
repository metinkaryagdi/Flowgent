namespace BitirmeProject.SprintService.Domain.Entities;

/// <summary>
/// Immutable velocity snapshot captured when a sprint is completed.
/// Once written, this record is never updated — it represents the final state of the sprint.
/// </summary>
public sealed class SprintSummary
{
    public Guid SprintId { get; private set; }
    public int TotalIssues { get; private set; }
    public int CompletedIssues { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public DateTime SnapshotTakenAt { get; private set; }

    private SprintSummary() { }

    public SprintSummary(Guid sprintId, int totalIssues, int completedIssues, DateTime completedAt)
    {
        SprintId = sprintId;
        TotalIssues = totalIssues;
        CompletedIssues = completedIssues;
        CompletedAt = completedAt;
        SnapshotTakenAt = DateTime.UtcNow;
    }
}
