using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Features.Sprints.Commands.SuggestBalance;

public sealed class SuggestBalanceCommandHandler
    : IRequestHandler<SuggestBalanceCommand, SuggestBalanceResultDto>
{
    private readonly IOllamaClient _ollama;
    private readonly ISprintServiceClient _sprintClient;
    private readonly IAiSessionRepository _sessions;
    private readonly ILogger<SuggestBalanceCommandHandler> _logger;

    public SuggestBalanceCommandHandler(
        IOllamaClient ollama,
        ISprintServiceClient sprintClient,
        IAiSessionRepository sessions,
        ILogger<SuggestBalanceCommandHandler> logger)
    {
        _ollama = ollama;
        _sprintClient = sprintClient;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<SuggestBalanceResultDto> Handle(
        SuggestBalanceCommand request, CancellationToken cancellationToken)
    {
        var session = new AiSession(
            request.ProjectId, request.UserId, request.OrganizationId,
            AiSessionType.BalanceSuggestion);
        session.MarkProcessing();
        await _sessions.AddAsync(session, cancellationToken);

        SprintDetailDto? sprint = null;
        try
        {
            sprint = await _sprintClient.GetSprintByIdAsync(request.SprintId, request.OrganizationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch sprint {SprintId} for balance suggestion", request.SprintId);
        }

        if (sprint is null || sprint.Issues.Count == 0)
        {
            session.Fail("No sprint data available");
            await _sessions.SaveChangesAsync(cancellationToken);
            return new SuggestBalanceResultDto
            {
                SessionId = session.Id,
                SprintId = request.SprintId,
                Analysis = "Sprint verisi bulunamadı.",
                Recommendation = "Issue'ları kontrol edin.",
                Suggestions = new()
            };
        }

        var prompt = BuildPrompt(sprint);
        OllamaBalanceResponse? parsed = null;
        try
        {
            parsed = await _ollama.GenerateJsonAsync<OllamaBalanceResponse>(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama balance suggestion failed for sprint {SprintId}", request.SprintId);
        }

        parsed ??= new OllamaBalanceResponse
        {
            Analysis = "Sprint yük analizi yapılamadı.",
            Recommendation = "Issue önceliklerini gözden geçirin.",
            Suggestions = new()
        };

        var result = new AiPlanResult(
            session.Id,
            prompt,
            rawResponse: parsed.Analysis,
            parsedJson: System.Text.Json.JsonSerializer.Serialize(parsed));
        await _sessions.AddResultAsync(result, cancellationToken);
        session.Complete(result);
        await _sessions.SaveChangesAsync(cancellationToken);

        return new SuggestBalanceResultDto
        {
            SessionId = session.Id,
            SprintId = request.SprintId,
            Analysis = parsed.Analysis,
            Recommendation = parsed.Recommendation,
            Suggestions = parsed.Suggestions.Select(s => new BalanceSuggestionItemDto
            {
                IssueTitle = s.IssueTitle,
                CurrentPriority = s.CurrentPriority,
                SuggestedAction = s.SuggestedAction
            }).ToList()
        };
    }

    private static string BuildPrompt(SprintDetailDto sprint)
    {
        var critical = sprint.Issues.Count(i => i.Priority.Equals("Critical", StringComparison.OrdinalIgnoreCase));
        var high = sprint.Issues.Count(i => i.Priority.Equals("High", StringComparison.OrdinalIgnoreCase));
        var medium = sprint.Issues.Count(i => i.Priority.Equals("Medium", StringComparison.OrdinalIgnoreCase));
        var low = sprint.Issues.Count(i => i.Priority.Equals("Low", StringComparison.OrdinalIgnoreCase));

        var issueLines = string.Join("\n", sprint.Issues.Select(i =>
            $"- [{i.Priority}] [{i.Status}] {i.Title}"));

        return $$"""
Sen BitirmeProject AI agent'ısın. Sprint iş yükü dengesini analiz eden bir scrum master gibi davranırsın.

Sprint: {{sprint.Name}}
Toplam Issue: {{sprint.Issues.Count}}
Öncelik dağılımı: Critical={{critical}}, High={{high}}, Medium={{medium}}, Low={{low}}

Issue'lar:
{{issueLines}}

Sprint'in aşırı yüklü veya dengesiz olup olmadığını değerlendir ve iyileştirme öner.

Kurallar:
- Yalnızca geçerli JSON döndür — markdown fence, açıklama, kod bloğu YASAK.
- Tüm metinler TÜRKÇE olmalı. İngilizce sızıntı yasak.
- En fazla 5 öneri ver. Critical ve High öncelikli aşırı yüklenmeye odaklan.
- "currentPriority" alanı issue'nun mevcut önceliği olmalı (Critical/High/Medium/Low).
- "suggestedAction" Türkçe ve eyleme dönük olmalı (örn. "ertele", "böl", "öncelik düşür").

Zorunlu JSON formatı:
{
  "analysis": "Sprint iş yükü dağılımının kısa Türkçe analizi",
  "recommendation": "Ekip için genel Türkçe öneri",
  "suggestions": [
    {"issueTitle": "...", "currentPriority": "...", "suggestedAction": "kısa Türkçe öneri"}
  ]
}
""";
    }
}
