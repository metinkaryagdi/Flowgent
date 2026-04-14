using BitirmeProject.AiService.Domain.Enums;
using Shared.Abstractions.Domain;

namespace BitirmeProject.AiService.Domain.Entities;

public sealed class AiSession : AggregateRoot<Guid>
{
    private readonly List<AiPlanResult> _results = new();

    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public AiSessionType Type { get; private set; }
    public AiSessionStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IReadOnlyCollection<AiPlanResult> Results => _results.AsReadOnly();

    private AiSession() { }

    public AiSession(Guid projectId, Guid userId, Guid organizationId, AiSessionType type)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        UserId = userId;
        OrganizationId = organizationId;
        Type = type;
        Status = AiSessionStatus.Pending;
    }

    public void MarkProcessing()
    {
        Status = AiSessionStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(AiPlanResult result)
    {
        _results.Add(result);
        Status = AiSessionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = AiSessionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
