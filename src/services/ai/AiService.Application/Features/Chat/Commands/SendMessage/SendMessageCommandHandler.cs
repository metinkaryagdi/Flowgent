using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Domain.Entities;
using BitirmeProject.AiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.AiService.Application.Features.Chat.Commands.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatResponseDto>
{
    private readonly IOllamaClient _ollama;
    private readonly ISprintServiceClient _sprintClient;
    private readonly IIssueServiceClient _issueClient;
    private readonly IAiSessionRepository _sessions;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IOllamaClient ollama,
        ISprintServiceClient sprintClient,
        IIssueServiceClient issueClient,
        IAiSessionRepository sessions,
        ILogger<SendMessageCommandHandler> logger)
    {
        _ollama = ollama;
        _sprintClient = sprintClient;
        _issueClient = issueClient;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<ChatResponseDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // Continue existing session or start new one
        AiSession session;
        if (request.SessionId.HasValue)
        {
            session = await _sessions.GetByIdAsync(request.SessionId.Value, cancellationToken)
                ?? throw new NotFoundException("AiSession", request.SessionId.Value);

            if (session.Type != AiSessionType.Chat
                || session.UserId != request.UserId
                || session.ProjectId != request.ProjectId
                || session.OrganizationId != request.OrganizationId)
            {
                throw new NotFoundException("AiSession", request.SessionId.Value);
            }
        }
        else
        {
            session = CreateNewSession(request);
            await _sessions.AddAsync(session, cancellationToken);
        }

        session.MarkProcessing();
        await _sessions.SaveChangesAsync(cancellationToken);

        // Build context from live data
        var context = await BuildContextAsync(request.ProjectId, request.OrganizationId, cancellationToken);

        // Build conversation history from existing results
        var history = session.Results
            .OrderBy(r => r.CreatedAt)
            .Select(r => $"User: {r.Prompt}\nAssistant: {r.RawResponse}")
            .ToList();

        var prompt = BuildPrompt(request.Message, context, history);
        string answer;

        try
        {
            answer = await _ollama.GenerateAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama chat failed for session {SessionId}", session.Id);
            session.Fail(ex.Message);
            await _sessions.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("AI model could not respond. Please try again.", ex);
        }

        // Save this turn — prompt = user message, rawResponse = answer
        var turn = new AiPlanResult(session.Id, request.Message, answer, null);
        session.Complete(turn);
        await _sessions.SaveChangesAsync(cancellationToken);

        return new ChatResponseDto
        {
            SessionId = session.Id,
            Message = request.Message,
            Answer = answer,
            Timestamp = DateTime.UtcNow
        };
    }

    private static AiSession CreateNewSession(SendMessageCommand request)
        => new(request.ProjectId, request.UserId, request.OrganizationId, AiSessionType.Chat);

    private async Task<string> BuildContextAsync(Guid projectId, Guid organizationId, CancellationToken ct)
    {
        var contextParts = new List<string>();

        try
        {
            var activeSprint = await _sprintClient.GetActiveSprintAsync(projectId, organizationId, ct);
            if (activeSprint is not null)
            {
                contextParts.Add($"Active Sprint: {activeSprint.Name} — Goal: {activeSprint.Goal ?? "none"}");
                foreach (var issue in activeSprint.Issues)
                    contextParts.Add($"  - [{issue.Status}] {issue.Title} (Priority: {issue.Priority})");
            }
            else
            {
                contextParts.Add("No active sprint.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch sprint context");
            contextParts.Add("Sprint data unavailable.");
        }

        try
        {
            var issues = await _issueClient.GetIssuesByProjectAsync(projectId, organizationId, ct);
            contextParts.Add($"Total issues in project: {issues.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch issue context");
        }

        return string.Join("\n", contextParts);
    }

    private static string BuildPrompt(string message, string context, List<string> history)
    {
        var historyBlock = history.Count > 0
            ? "Previous conversation:\n" + string.Join("\n\n", history.TakeLast(5)) + "\n\n"
            : string.Empty;

        return $$"""
            You are a helpful software project assistant. Answer the user's question based on the provided project context.
            Be concise and helpful. If the answer is not in the context, say so honestly.

            Project context:
            {{context}}

            {{historyBlock}}User: {{message}}
            Assistant:
            """;
    }
}
