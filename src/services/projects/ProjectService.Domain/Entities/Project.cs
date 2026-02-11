using Shared.Abstractions.Domain;

namespace BitirmeProject.ProjectService.Domain.Entities;

public sealed class Project : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public bool IsArchived { get; private set; }

    public int IssueCount { get; private set; }
    public int OpenIssueCount { get; private set; }
    public int InProgressIssueCount { get; private set; }
    public int DoneIssueCount { get; private set; }

    private Project() { }

    public Project(string name, string key, Guid ownerUserId)
    {
        Id = Guid.NewGuid();
        SetName(name);
        SetKey(key);
        OwnerUserId = ownerUserId;
        IsArchived = false;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty.", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Project key cannot be empty.", nameof(key));

        Key = key.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
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

        if (string.IsNullOrWhiteSpace(newKey))
            return;

        if (oldKey == newKey)
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