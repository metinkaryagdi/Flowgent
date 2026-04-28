using Shared.Abstractions.Domain;

namespace BitirmeProject.AiService.Domain.Entities;

public sealed class AiToolExecution : Entity<Guid>
{
    public Guid? SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string ToolName { get; private set; } = null!;
    public string InputJson { get; private set; } = null!;
    public string? OutputJson { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long DurationMs { get; private set; }

    private AiToolExecution() { }

    public AiToolExecution(
        Guid? sessionId,
        Guid userId,
        Guid organizationId,
        Guid projectId,
        string toolName,
        string inputJson,
        string? outputJson,
        bool success,
        string? errorMessage,
        long durationMs)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        UserId = userId;
        OrganizationId = organizationId;
        ProjectId = projectId;
        ToolName = toolName;
        InputJson = inputJson;
        OutputJson = outputJson;
        Success = success;
        ErrorMessage = errorMessage;
        DurationMs = durationMs;
    }
}
