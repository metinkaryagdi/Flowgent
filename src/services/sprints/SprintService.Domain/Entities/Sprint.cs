using Shared.Abstractions.Domain;
using Shared.Abstractions.Exceptions;
using BitirmeProject.SprintService.Domain.Enums;

namespace BitirmeProject.SprintService.Domain.Entities;

public sealed class Sprint : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public SprintStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Sprint() { }

    public Sprint(Guid projectId, string name, string? goal, Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        SetName(name);
        SetGoal(goal);
        Status = SprintStatus.Planned;
        CreatedByUserId = createdByUserId;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sprint name cannot be empty.", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetGoal(string? goal)
    {
        Goal = string.IsNullOrWhiteSpace(goal) ? null : goal.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        if (Status != SprintStatus.Planned)
            throw new BusinessRuleException($"Cannot start sprint in status {Status}.");

        Status = SprintStatus.Active;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != SprintStatus.Active)
            throw new BusinessRuleException($"Cannot complete sprint in status {Status}.");

        Status = SprintStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
