using Shared.Abstractions.Domain;
using Shared.Abstractions.Exceptions;
using BitirmeProject.SprintService.Domain.Enums;

namespace BitirmeProject.SprintService.Domain.Entities;

public sealed class Sprint : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public SprintStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Sprint() { }

    public Sprint(Guid projectId, string name, string? goal, DateTime startDate, DateTime endDate, Guid createdByUserId, Guid? organizationId = null)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        OrganizationId = organizationId;
        SetName(name);
        SetGoal(goal);
        SetSchedule(startDate, endDate);
        Status = SprintStatus.Planned;
        CreatedByUserId = createdByUserId;
    }

    public void SetName(string name)
    {
        EnsureMutable();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Sprint name cannot be empty.", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetGoal(string? goal)
    {
        EnsureMutable();

        Goal = string.IsNullOrWhiteSpace(goal) ? null : goal.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSchedule(DateTime startDate, DateTime endDate)
    {
        EnsureMutable();

        if (startDate == default)
            throw new ArgumentException("Sprint start date is required.", nameof(startDate));

        if (endDate == default)
            throw new ArgumentException("Sprint end date is required.", nameof(endDate));

        if (endDate <= startDate)
            throw new BusinessRuleException("Sprint end date must be later than start date.");

        StartDate = startDate;
        EndDate = endDate;
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

    private void EnsureMutable()
    {
        if (Status == SprintStatus.Completed)
            throw new BusinessRuleException("Completed sprints are immutable.");
    }
}
