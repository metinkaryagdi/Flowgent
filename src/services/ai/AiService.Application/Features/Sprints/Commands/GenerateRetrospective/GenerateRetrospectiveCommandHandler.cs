using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Features.Sprints.Commands.GenerateRetrospective;

public sealed class GenerateRetrospectiveCommandHandler
    : IRequestHandler<GenerateRetrospectiveCommand, RetrospectiveResultDto>
{
    private readonly IOllamaClient _ollama;
    private readonly ISprintServiceClient _sprintClient;
    private readonly IAiSessionRepository _sessions;
    private readonly ILogger<GenerateRetrospectiveCommandHandler> _logger;

    public GenerateRetrospectiveCommandHandler(
        IOllamaClient ollama,
        ISprintServiceClient sprintClient,
        IAiSessionRepository sessions,
        ILogger<GenerateRetrospectiveCommandHandler> logger)
    {
        _ollama = ollama;
        _sprintClient = sprintClient;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<RetrospectiveResultDto> Handle(
        GenerateRetrospectiveCommand request, CancellationToken cancellationToken)
    {
        var session = new AiSession(
            request.ProjectId, request.UserId, request.OrganizationId,
            AiSessionType.Retrospective);
        session.MarkProcessing();
        await _sessions.AddAsync(session, cancellationToken);

        SprintDetailDto? sprint = null;
        try
        {
            sprint = await _sprintClient.GetSprintByIdAsync(request.SprintId, request.OrganizationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch sprint {SprintId} for retrospective", request.SprintId);
        }

        var prompt = BuildPrompt(request.SprintId, sprint);

        OllamaRetrospectiveResponse? parsed = null;
        try
        {
            parsed = await _ollama.GenerateJsonAsync<OllamaRetrospectiveResponse>(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama retrospective generation failed for sprint {SprintId}", request.SprintId);
        }

        parsed ??= new OllamaRetrospectiveResponse
        {
            Summary = "Sprint tamamlandı.",
            WentWell = "İssue'lar teslim edildi.",
            Improvements = "Süreçler iyileştirilebilir.",
            ActionItems = "Takım ile toplantı yapın."
        };

        var result = new AiPlanResult(
            session.Id,
            prompt,
            rawResponse: parsed.Summary,
            parsedJson: System.Text.Json.JsonSerializer.Serialize(parsed));
        await _sessions.AddResultAsync(result, cancellationToken);
        session.Complete(result);
        await _sessions.SaveChangesAsync(cancellationToken);

        return new RetrospectiveResultDto
        {
            SessionId = session.Id,
            SprintId = request.SprintId,
            Summary = parsed.Summary,
            WentWell = parsed.WentWell,
            Improvements = parsed.Improvements,
            ActionItems = parsed.ActionItems
        };
    }

    private static string BuildPrompt(Guid sprintId, SprintDetailDto? sprint)
    {
        if (sprint is null)
        {
            return $$"""
Sen BitirmeProject AI agent'ısın. Sprint retrospektifi üreten bir scrum master gibi davranırsın.
Sprint ID: {{sprintId}}
Bu sprint için detaylı veri bulunamadı.

Kurallar:
- Yalnızca geçerli JSON döndür — markdown fence, açıklama, kod bloğu YASAK.
- Tüm metinler TÜRKÇE olmalı. İngilizce sızıntı yasak.

Zorunlu JSON formatı:
{"summary":"...","wentWell":"...","improvements":"...","actionItems":"..."}
""";
        }

        var total = sprint.Issues.Count;
        var done = sprint.Issues.Count(i => i.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));
        var inProgress = sprint.Issues.Count(i => i.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) || i.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase));
        var open = total - done - inProgress;

        var issueLines = string.Join("\n", sprint.Issues.Select(i =>
            $"- [{i.Status}] [{i.Priority}] {i.Title}"));

        return $$"""
Sen BitirmeProject AI agent'ısın. Tamamlanan sprint için retrospektif üretirsin.

Sprint: {{sprint.Name}}
Hedef: {{sprint.Goal ?? "Hedef tanımlanmamış"}}
Toplam Issue: {{total}} | Tamamlanan: {{done}} | Devam Eden: {{inProgress}} | Açık: {{open}}
Tamamlanma Oranı: {{(total > 0 ? (done * 100 / total) : 0)}}%

Issue'lar:
{{issueLines}}

Kurallar:
- Yalnızca geçerli JSON döndür — markdown fence, açıklama, kod bloğu YASAK.
- Tüm metinler TÜRKÇE olmalı. İngilizce sızıntı yasak.

Zorunlu JSON formatı:
{
  "summary": "Sprint'in 2-3 cümlelik Türkçe özeti",
  "wentWell": "Bu sprint'te neyin iyi gittiği (Türkçe)",
  "improvements": "Neyin iyileştirilebileceği (Türkçe)",
  "actionItems": "Ekip için somut sonraki adımlar (Türkçe)"
}
""";
    }
}
