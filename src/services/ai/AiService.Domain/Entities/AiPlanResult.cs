using Shared.Abstractions.Domain;

namespace BitirmeProject.AiService.Domain.Entities;

public sealed class AiPlanResult : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public string Prompt { get; private set; } = null!;
    public string RawResponse { get; private set; } = null!;
    public string? ParsedJson { get; private set; }
    public bool WasApplied { get; private set; }

    public AiSession Session { get; private set; } = null!;

    private AiPlanResult() { }

    public AiPlanResult(Guid sessionId, string prompt, string rawResponse, string? parsedJson)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        Prompt = prompt;
        RawResponse = rawResponse;
        ParsedJson = parsedJson;
        WasApplied = false;
    }

    public void MarkApplied()
    {
        WasApplied = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
