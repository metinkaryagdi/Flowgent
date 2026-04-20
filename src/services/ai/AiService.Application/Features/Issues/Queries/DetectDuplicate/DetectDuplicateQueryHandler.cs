using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.Common;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BitirmeProject.AiService.Application.Features.Issues.Queries.DetectDuplicate;

public sealed class DetectDuplicateQueryHandler : IRequestHandler<DetectDuplicateQuery, DetectDuplicateResultDto>
{
    private readonly IOllamaClient _ollama;
    private readonly IIssueServiceClient _issueClient;
    private readonly IAiSessionRepository _sessions;
    private readonly ILogger<DetectDuplicateQueryHandler> _logger;

    public DetectDuplicateQueryHandler(
        IOllamaClient ollama,
        IIssueServiceClient issueClient,
        IAiSessionRepository sessions,
        ILogger<DetectDuplicateQueryHandler> logger)
    {
        _ollama = ollama;
        _issueClient = issueClient;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<DetectDuplicateResultDto> Handle(DetectDuplicateQuery request, CancellationToken cancellationToken)
    {
        var existingIssues = await _issueClient.GetIssuesByProjectAsync(
            request.ProjectId, request.OrganizationId, cancellationToken);

        if (existingIssues.Count == 0)
            return new DetectDuplicateResultDto { SessionId = Guid.NewGuid() };

        var session = new AiSession(request.ProjectId, request.UserId, request.OrganizationId, AiSessionType.IssueEnrichment);
        await _sessions.AddAsync(session, cancellationToken);
        session.MarkProcessing();
        await _sessions.SaveChangesAsync(cancellationToken);

        var prompt = BuildPrompt(PromptSanitizer.Sanitize(request.Title), existingIssues);
        OllamaDuplicateResponse? response;

        try
        {
            response = await _ollama.GenerateJsonAsync<OllamaDuplicateResponse>(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama duplicate detection failed");
            session.Fail(ex.Message);
            await _sessions.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("AI model could not check for duplicates.", ex);
        }

        var rawJson = JsonSerializer.Serialize(response);
        var planResult = new AiPlanResult(session.Id, prompt, rawJson, rawJson);
        session.Complete(planResult);
        await _sessions.SaveChangesAsync(cancellationToken);

        var similar = response?.SimilarIssues
            .Where(s => Guid.TryParse(s.IssueId, out _))
            .Select(s => new SimilarIssueDto
            {
                IssueId = Guid.Parse(s.IssueId),
                Title = s.Title,
                Reason = s.Reason,
                SimilarityScore = s.SimilarityScore
            })
            .ToList() ?? new();

        return new DetectDuplicateResultDto
        {
            SessionId = session.Id,
            SimilarIssues = similar
        };
    }

    private static string BuildPrompt(string newTitle, List<ProjectIssueDto> existing)
    {
        var issueList = string.Join("\n", existing.Select(i => $"- id:{i.Id} | {i.Title}"));

        return $$"""
            You are a software project assistant. Check if the new issue title is similar or duplicate to any existing issues.

            Rules:
            - Return ONLY valid JSON — no markdown, no explanation, no code blocks.
            - Only include issues with similarityScore >= 60.
            - similarityScore is an integer from 0 to 100.
            - issueId must be the exact GUID from the list.
            - If there are no similar issues, return { "similarIssues": [] }.

            Required JSON format:
            {
              "similarIssues": [
                {
                  "issueId": "guid-here",
                  "title": "existing issue title",
                  "reason": "why it might be a duplicate",
                  "similarityScore": 85
                }
              ]
            }

            New issue title: {{newTitle}}

            Existing issues:
            {{issueList}}
            """;
    }
}
