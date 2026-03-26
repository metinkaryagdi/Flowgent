using Shared.Abstractions.Domain;

namespace BitirmeProject.ProjectService.Domain.Entities;

public sealed class Project : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public bool IsArchived { get; private set; }

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
}
