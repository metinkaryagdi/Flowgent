using BitirmeProject.AiService.Application.Abstractions;
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
        await _sessions.AddAsync(session, cancellationToken);
        session.MarkProcessing();
        await _sessions.SaveChangesAsync(cancellationToken);

        var prompt = BuildPrompt(request.Title);
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
        You are a software project assistant. Enrich the following issue title into a complete issue specification.

        Rules:
        - Return ONLY valid JSON — no markdown, no explanation, no code blocks.
        - storyPoints must be an integer between 1 and 13 (Fibonacci: 1,2,3,5,8,13).
        - Be concise but complete.

        Required JSON format:
        {
          "description": "Detailed description of what needs to be implemented",
          "acceptanceCriteria": "- Criterion 1\n- Criterion 2\n- Criterion 3",
          "edgeCases": "- Edge case 1\n- Edge case 2",
          "storyPoints": 3
        }

        Issue title: {{title}}
        """;
}
