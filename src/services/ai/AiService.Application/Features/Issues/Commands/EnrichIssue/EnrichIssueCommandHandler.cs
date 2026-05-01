using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.Common;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Features.Issues.Commands.EnrichIssue;

public sealed class EnrichIssueCommandHandler : IRequestHandler<EnrichIssueCommand, EnrichIssueResultDto>
{
    private readonly IOllamaClient _ollama;
    private readonly IAiSessionRepository _sessions;
    private readonly ILogger<EnrichIssueCommandHandler> _logger;

    public EnrichIssueCommandHandler(
        IOllamaClient ollama,
        IAiSessionRepository sessions,
        ILogger<EnrichIssueCommandHandler> logger)
    {
        _ollama = ollama;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<EnrichIssueResultDto> Handle(EnrichIssueCommand request, CancellationToken cancellationToken)
    {
        var session = new AiSession(request.ProjectId, request.UserId, request.OrganizationId, AiSessionType.IssueEnrichment);
        session.MarkProcessing();
        await _sessions.AddAsync(session, cancellationToken);

        var prompt = BuildPrompt(PromptSanitizer.Sanitize(request.Title));
        OllamaEnrichResponse? enriched;

        try
        {
            enriched = await _ollama.GenerateJsonAsync<OllamaEnrichResponse>(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama enrichment failed for issue {IssueId}", request.IssueId);
            session.Fail(ex.Message);
            await _sessions.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("AI model could not enrich the issue. Please try again.", ex);
        }

        if (enriched is null)
        {
            session.Fail("AI returned unparseable response.");
            await _sessions.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("AI returned an empty response. Try rephrasing the title.");
        }

        var rawJson = System.Text.Json.JsonSerializer.Serialize(enriched);
        var result = new AiPlanResult(session.Id, prompt, rawJson, rawJson);
        result.MarkApplied();
        await _sessions.AddResultAsync(result, cancellationToken);
        session.Complete(result);
        await _sessions.SaveChangesAsync(cancellationToken);

        return new EnrichIssueResultDto
        {
            SessionId = session.Id,
            Description = enriched.Description,
            AcceptanceCriteria = enriched.AcceptanceCriteria,
            EdgeCases = enriched.EdgeCases,
            StoryPoints = enriched.StoryPoints
        };
    }

    private static string BuildPrompt(string title) => $$"""
        Sen BitirmeProject AI agent'ısın. Aşağıdaki issue başlığını eksiksiz bir issue tanımına zenginleştirirsin.

        Kurallar:
        - Yalnızca geçerli JSON döndür — markdown fence, açıklama, kod bloğu YASAK.
        - Tüm metinler TÜRKÇE olmalı. İngilizce sızıntı yasak.
        - storyPoints 1-13 arası tam sayı olmalı (Fibonacci: 1,2,3,5,8,13).
        - Kısa ama eksiksiz ol.

        Zorunlu JSON formatı:
        {
          "description": "Yapılması gerekenin ayrıntılı Türkçe açıklaması",
          "acceptanceCriteria": "- Kriter 1\n- Kriter 2\n- Kriter 3",
          "edgeCases": "- Uç durum 1\n- Uç durum 2",
          "storyPoints": 3
        }

        Issue başlığı: {{title}}
        """;
}
