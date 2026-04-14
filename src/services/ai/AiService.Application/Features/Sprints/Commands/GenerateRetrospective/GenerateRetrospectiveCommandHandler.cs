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
        await _sessions.SaveChangesAsync(cancellationToken);

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
You are a scrum master assistant generating a sprint retrospective.
Sprint ID: {{sprintId}}
No detailed data was available for this sprint.

Generate a generic sprint retrospective in JSON:
{"summary":"...","wentWell":"...","improvements":"...","actionItems":"..."}
Respond ONLY with the JSON object, no markdown.
""";
        }

        var total = sprint.Issues.Count;
        var done = sprint.Issues.Count(i => i.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));
        var inProgress = sprint.Issues.Count(i => i.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) || i.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase));
        var open = total - done - inProgress;

        var issueLines = string.Join("\n", sprint.Issues.Select(i =>
            $"- [{i.Status}] [{i.Priority}] {i.Title}"));

        return $$"""
You are a scrum master assistant. Generate a retrospective for the completed sprint.

Sprint: {{sprint.Name}}
Goal: {{sprint.Goal ?? "No goal set"}}
Total Issues: {{total}} | Done: {{done}} | In Progress: {{inProgress}} | Open: {{open}}
Completion Rate: {{(total > 0 ? (done * 100 / total) : 0)}}%

Issues:
{{issueLines}}

Generate a concise retrospective in JSON format:
{
  "summary": "2-3 sentence sprint summary",
  "wentWell": "What went well in this sprint",
  "improvements": "What could be improved",
  "actionItems": "Concrete next steps for the team"
}
Respond ONLY with the JSON object, no markdown, no explanation.
""";
    }
}
