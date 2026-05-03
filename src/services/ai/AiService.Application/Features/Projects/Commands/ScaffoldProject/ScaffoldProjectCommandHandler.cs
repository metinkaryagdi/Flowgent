using System.Text.RegularExpressions;
using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.Common;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Features.Projects.Commands.ScaffoldProject;

public sealed class ScaffoldProjectCommandHandler : IRequestHandler<ScaffoldProjectCommand, ProjectScaffoldDraftDto>
{
    private readonly IOllamaClient _ollama;
    private readonly IAiSessionRepository _sessionRepository;
    private readonly ILogger<ScaffoldProjectCommandHandler> _logger;

    private static readonly HashSet<string> ValidPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High", "Critical"
    };

    public ScaffoldProjectCommandHandler(
        IOllamaClient ollama,
        IAiSessionRepository sessionRepository,
        ILogger<ScaffoldProjectCommandHandler> logger)
    {
        _ollama = ollama;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<ProjectScaffoldDraftDto> Handle(ScaffoldProjectCommand request, CancellationToken ct)
    {
        var session = new AiSession(Guid.Empty, request.UserId, request.OrganizationId, AiSessionType.ProjectScaffold);
        session.MarkProcessing();

        var prompt = BuildPrompt(PromptSanitizer.Sanitize(request.Description));
        OllamaScaffoldResponse? scaffold;
        string rawResponse;

        try
        {
            scaffold = await _ollama.GenerateJsonAsync<OllamaScaffoldResponse>(prompt, ct);
            rawResponse = System.Text.Json.JsonSerializer.Serialize(scaffold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama scaffold request failed");
            session.Fail(ex.Message);
            await _sessionRepository.AddAsync(session, ct);
            await _sessionRepository.SaveChangesAsync(ct);
            throw new InvalidOperationException("AI taslak üretemedi. Lütfen tekrar dene.", ex);
        }

        if (scaffold is null || scaffold.Sprints.Count == 0 || string.IsNullOrWhiteSpace(scaffold.ProjectName))
        {
            _logger.LogWarning("Scaffold parse fail. Raw (ilk 600): {Raw}", rawResponse.Length > 600 ? rawResponse[..600] : rawResponse);
            session.Fail("AI returned an empty or unparseable scaffold.");
            await _sessionRepository.AddAsync(session, ct);
            await _sessionRepository.SaveChangesAsync(ct);
            throw new InvalidOperationException("AI boş taslak döndürdü. Açıklamayı yeniden ifade edip tekrar dene.");
        }

        var draft = new ProjectScaffoldDraftDto
        {
            SessionId = session.Id,
            ProjectName = scaffold.ProjectName.Trim(),
            ProjectKey = NormalizeKey(scaffold.ProjectKey, scaffold.ProjectName),
            Description = scaffold.Description?.Trim() ?? string.Empty,
            Sprints = scaffold.Sprints.Select(s => new DraftSprintDto
            {
                Name = s.Name?.Trim() ?? "Sprint",
                Goal = s.Goal?.Trim() ?? string.Empty,
                Issues = s.Issues.Select(i => new DraftIssueDto
                {
                    Title = i.Title?.Trim() ?? string.Empty,
                    Description = i.Description?.Trim() ?? string.Empty,
                    Priority = ValidPriorities.Contains(i.Priority) ? CapitalizeFirst(i.Priority) : "Medium"
                }).Where(i => !string.IsNullOrWhiteSpace(i.Title)).ToList()
            }).ToList()
        };

        var result = new AiPlanResult(session.Id, prompt, rawResponse, rawResponse);
        result.MarkApplied();
        session.Complete(result);

        await _sessionRepository.AddAsync(session, ct);
        await _sessionRepository.SaveChangesAsync(ct);

        return draft;
    }

    private static string NormalizeKey(string? raw, string projectName)
    {
        var candidate = (raw ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(candidate))
        {
            candidate = Regex.Replace(candidate, @"[^A-Za-z0-9]", string.Empty).ToUpperInvariant();
            if (candidate.Length >= 2 && candidate.Length <= 10)
                return candidate;
        }

        // Fallback: ilk kelime + harfler
        var letters = Regex.Replace(projectName ?? string.Empty, @"[^A-Za-z]", string.Empty).ToUpperInvariant();
        if (letters.Length == 0) return "PRJ";
        return letters.Length > 4 ? letters[..4] : letters;
    }

    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

    private static string BuildPrompt(string description) => $$"""
        Sen BitirmeProject AI agent'ısın. Aşağıdaki açıklamadan eksiksiz bir proje iskeleti üretirsin.

        Kurallar:
        - Yalnızca geçerli JSON döndür. Markdown, kod bloğu, açıklama YASAK.
        - Project key: 2-4 büyük harf, isimden türetilmiş (örn. "ECOM", "CRM", "DEPO").
        - **Tam olarak 3 ile 5 arasında sprint üret. Her sprint en az 5, en fazla 8 issue içermeli.** Daha azını üretme.
        - Issue önceliği yalnızca şunlardan biri olmalı: Low, Medium, High, Critical.
        - Issue başlıkları somut ve spesifik olmalı — "Veri girişi" gibi jenerik placeholder yerine "Ürün barkod tarayıcı entegrasyonu" gibi.
        - Issue açıklamaları en az 1-2 cümle olmalı; ne yapılacağı net belirtilmeli.
        - Sprint isimleri kısa ve açıklayıcı (örn. "Sprint 1: Temel Altyapı").
        - Proje adı, açıklama, sprint adları/hedefleri ve issue başlıkları/açıklamaları TÜRKÇE olmalı.

        Required JSON shape:
        {
          "projectName": "Türkçe proje adı",
          "projectKey": "ECOM",
          "description": "Bir cümlelik proje özeti (Türkçe)",
          "sprints": [
            {
              "name": "Sprint 1: ...",
              "goal": "Tek cümlelik sprint hedefi",
              "issues": [
                { "title": "...", "description": "...", "priority": "Medium" }
              ]
            }
          ]
        }

        Project description: {{description}}
        """;
}
