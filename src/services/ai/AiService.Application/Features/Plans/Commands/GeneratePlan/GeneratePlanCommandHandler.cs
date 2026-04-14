using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Features.Plans.Commands.GeneratePlan;

public sealed class GeneratePlanCommandHandler : IRequestHandler<GeneratePlanCommand, GeneratePlanResultDto>
{
    private readonly IOllamaClient _ollama;
    private readonly ISprintServiceClient _sprintClient;
    private readonly IIssueServiceClient _issueClient;
    private readonly IAiSessionRepository _sessionRepository;
    private readonly ILogger<GeneratePlanCommandHandler> _logger;

    public GeneratePlanCommandHandler(
        IOllamaClient ollama,
        ISprintServiceClient sprintClient,
        IIssueServiceClient issueClient,
        IAiSessionRepository sessionRepository,
        ILogger<GeneratePlanCommandHandler> logger)
    {
        _ollama = ollama;
        _sprintClient = sprintClient;
        _issueClient = issueClient;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task<GeneratePlanResultDto> Handle(GeneratePlanCommand request, CancellationToken cancellationToken)
    {
        var session = new AiSession(request.ProjectId, request.UserId, request.OrganizationId, AiSessionType.PlanGeneration);
        await _sessionRepository.AddAsync(session, cancellationToken);
        session.MarkProcessing();
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        string prompt = BuildPrompt(request.Description);
        string rawResponse;
        OllamaPlanResponse? plan;

        try
        {
            plan = await _ollama.GenerateJsonAsync<OllamaPlanResponse>(prompt, cancellationToken);
            rawResponse = System.Text.Json.JsonSerializer.Serialize(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama request failed for session {SessionId}", session.Id);
            session.Fail(ex.Message);
            await _sessionRepository.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("AI model could not generate a plan. Please try again.", ex);
        }

        if (plan is null || plan.Sprints.Count == 0)
        {
            session.Fail("AI returned an empty or unparseable plan.");
            await _sessionRepository.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("AI returned an empty plan. Try rephrasing the description.");
        }

        var createdSprints = new List<CreatedSprintDto>();

        foreach (var sprintPlan in plan.Sprints)
        {
            var sprint = await _sprintClient.CreateSprintAsync(
                request.ProjectId, request.UserId, request.OrganizationId, sprintPlan.Name, sprintPlan.Goal, cancellationToken);

            var createdIssues = new List<CreatedIssueDto>();

            foreach (var issuePlan in sprintPlan.Issues)
            {
                var issue = await _issueClient.CreateIssueAsync(
                    request.ProjectId, request.UserId, request.OrganizationId,
                    issuePlan.Title, issuePlan.Description, issuePlan.Priority, cancellationToken);

                await _sprintClient.AddIssueToSprintAsync(sprint.Id, issue.Id, request.UserId, cancellationToken);

                createdIssues.Add(issue);
            }

            createdSprints.Add(new CreatedSprintDto
            {
                Id = sprint.Id,
                Name = sprint.Name,
                Goal = sprint.Goal,
                Issues = createdIssues
            });
        }

        var result = new AiPlanResult(session.Id, prompt, rawResponse, rawResponse);
        result.MarkApplied();
        session.Complete(result);
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return new GeneratePlanResultDto
        {
            SessionId = session.Id,
            Sprints = createdSprints
        };
    }

    private static string BuildPrompt(string description) => $$"""
        You are a software project planning assistant. Your task is to create a sprint plan based on the project description below.

        Rules:
        - Return ONLY valid JSON — no markdown, no explanation, no code blocks.
        - Create 2-4 sprints, each with 3-6 issues.
        - Issue priorities must be one of: Low, Medium, High, Critical.
        - Keep sprint names concise (e.g. "Sprint 1: Foundation").
        - Keep issue titles short and actionable (e.g. "Create user login page").

        Required JSON format:
        {
          "sprints": [
            {
              "name": "Sprint 1: ...",
              "goal": "One-sentence sprint goal",
              "issues": [
                {
                  "title": "...",
                  "description": "...",
                  "priority": "Medium",
                  "storyPoints": 3
                }
              ]
            }
          ]
        }

        Project description: {{description}}
        """;
}
