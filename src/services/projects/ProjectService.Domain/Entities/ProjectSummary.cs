namespace BitirmeProject.ProjectService.Domain.Entities;

public sealed class ProjectSummary
{
    public Guid ProjectId { get; private set; }
    public int IssueCount { get; private set; }
    public int OpenIssueCount { get; private set; }
    public int InProgressIssueCount { get; private set; }
    public int DoneIssueCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ProjectSummary() { }

    public ProjectSummary(Guid projectId)
    {
        ProjectId = projectId;
        CreatedAt = DateTime.UtcNow;
    }

    public void RegisterIssueCreated()
    {
        IssueCount += 1;
        OpenIssueCount += 1;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyIssueStatusChange(string? oldStatus, string? newStatus)
    {
        var oldKey = NormalizeStatus(oldStatus);
        var newKey = NormalizeStatus(newStatus);

        if (string.IsNullOrWhiteSpace(newKey) || oldKey == newKey)
            return;

        Decrement(oldKey);
        Increment(newKey);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "open" => "open",
            "inprogress" => "inprogress",
            "in_progress" => "inprogress",
            "in-progress" => "inprogress",
            "done" => "done",
            _ => string.Empty
        };
    }

    private void Increment(string key)
    {
        switch (key)
        {
            case "open":
                OpenIssueCount += 1;
                break;
            case "inprogress":
                InProgressIssueCount += 1;
                break;
            case "done":
                DoneIssueCount += 1;
                break;
        }
    }

    private void Decrement(string key)
    {
        switch (key)
        {
            case "open":
                if (OpenIssueCount > 0) OpenIssueCount -= 1;
                break;
            case "inprogress":
                if (InProgressIssueCount > 0) InProgressIssueCount -= 1;
                break;
            case "done":
                if (DoneIssueCount > 0) DoneIssueCount -= 1;
                break;
        }
    }
}
