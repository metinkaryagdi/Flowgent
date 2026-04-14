using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Features.Sprints.Queries.AssessRisk;

public sealed class AssessSprintRiskQueryHandler
    : IRequestHandler<AssessSprintRiskQuery, SprintRiskResultDto>
{
    private readonly IOllamaClient _ollama;
    private readonly ISprintServiceClient _sprintClient;
    private readonly IAiSessionRepository _sessions;
    private readonly ILogger<AssessSprintRiskQueryHandler> _logger;

    public AssessSprintRiskQueryHandler(
        IOllamaClient ollama,
        ISprintServiceClient sprintClient,
        IAiSessionRepository sessions,
        ILogger<AssessSprintRiskQueryHandler> logger)
    {
        _ollama = ollama;
        _sprintClient = sprintClient;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<SprintRiskResultDto> Handle(
        AssessSprintRiskQuery request, CancellationToken cancellationToken)
    {
        var session = new AiSession(
            request.ProjectId, request.UserId, request.OrganizationId,
            AiSessionType.RiskAssessment);
        session.MarkProcessing();
        await _sessions.AddAsync(session, cancellationToken);
        await _sessions.SaveChangesAsync(cancellationToken);

        SprintDetailDto? sprint = null;
        try
        {
            sprint = await _sprintClient.GetSprintByIdAsync(request.SprintId, request.OrganizationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch sprint {SprintId} for risk assessment", request.SprintId);
        }

        if (sprint is null || sprint.Issues.Count == 0)
        {
            session.Fail("No sprint data available");
            await _sessions.SaveChangesAsync(cancellationToken);
            return new SprintRiskResultDto
            {
                SessionId = session.Id,
                SprintId = request.SprintId,
                RiskLevel = "Unknown",
                Reason = "Sprint verisi bulunamadı.",
                Recommendation = "Issue'ları kontrol edin.",
                TotalIssues = 0,
                DoneIssues = 0,
                InProgressIssues = 0,
                OpenIssues = 0
            };
        }

        var total = sprint.Issues.Count;
        var done = sprint.Issues.Count(i => i.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));
        var inProgress = sprint.Issues.Count(i =>
            i.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) ||
            i.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase));
        var open = total - done - inProgress;
        var criticalOpen = sprint.Issues.Count(i =>
            i.Priority.Equals("Critical", StringComparison.OrdinalIgnoreCase) &&
            !i.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));

        var prompt = $$"""
You are a risk assessment specialist for agile sprints.

Sprint: {{sprint.Name}}
Total Issues: {{total}} | Done: {{done}} | In Progress: {{inProgress}} | Open: {{open}}
Critical issues not done: {{criticalOpen}}
Completion Rate: {{(total > 0 ? (done * 100 / total) : 0)}}%

Assess the sprint delay risk. Respond ONLY with this JSON (no markdown):
{
  "riskLevel": "Low|Medium|High|Critical",
  "reason": "Brief explanation of the risk level",
  "recommendation": "What the team should do to mitigate the risk"
}
""";

        OllamaRiskResponse? parsed = null;
        try
        {
            parsed = await _ollama.GenerateJsonAsync<OllamaRiskResponse>(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama risk assessment failed for sprint {SprintId}", request.SprintId);
        }

        // Derive a simple rule-based fallback if Ollama fails
        parsed ??= DeriveRuleBasedRisk(total, done, criticalOpen);

        var planResult = new AiPlanResult(
            session.Id,
            prompt,
            rawResponse: parsed.RiskLevel,
            parsedJson: System.Text.Json.JsonSerializer.Serialize(parsed));
        session.Complete(planResult);
        await _sessions.SaveChangesAsync(cancellationToken);

        return new SprintRiskResultDto
        {
            SessionId = session.Id,
            SprintId = request.SprintId,
            RiskLevel = parsed.RiskLevel,
            Reason = parsed.Reason,
            Recommendation = parsed.Recommendation,
            TotalIssues = total,
            DoneIssues = done,
            InProgressIssues = inProgress,
            OpenIssues = open
        };
    }

    private static OllamaRiskResponse DeriveRuleBasedRisk(int total, int done, int criticalOpen)
    {
        if (total == 0) return new OllamaRiskResponse { RiskLevel = "Low", Reason = "Sprint boş.", Recommendation = "Issue ekleyin." };

        var completionRate = done * 100 / total;
        if (criticalOpen > 0)
            return new OllamaRiskResponse { RiskLevel = "Critical", Reason = $"{criticalOpen} kritik issue tamamlanmadı.", Recommendation = "Kritik issue'lara öncelik verin." };
        if (completionRate < 30)
            return new OllamaRiskResponse { RiskLevel = "High", Reason = $"Tamamlanma oranı %{completionRate}.", Recommendation = "Backlog'u gözden geçirin, kapsamı daraltın." };
        if (completionRate < 60)
            return new OllamaRiskResponse { RiskLevel = "Medium", Reason = $"Tamamlanma oranı %{completionRate}.", Recommendation = "Engelleri kaldırın, odaklanmayı artırın." };
        return new OllamaRiskResponse { RiskLevel = "Low", Reason = $"Tamamlanma oranı %{completionRate}.", Recommendation = "Mevcut hızı koruyun." };
    }
}
