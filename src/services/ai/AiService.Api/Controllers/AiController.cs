using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Application.DTOs;
using BitirmeProject.AiService.Application.Features.Chat.Commands.SendMessage;
using BitirmeProject.AiService.Application.Features.Issues.Commands.EnrichIssue;
using BitirmeProject.AiService.Application.Features.Issues.Queries.DetectDuplicate;
using BitirmeProject.AiService.Application.Features.Plans.Commands.GeneratePlan;
using BitirmeProject.AiService.Application.Features.Projects.Commands.ScaffoldProject;
using BitirmeProject.AiService.Application.Features.Sprints.Commands.GenerateRetrospective;
using BitirmeProject.AiService.Application.Features.Sprints.Commands.SuggestBalance;
using BitirmeProject.AiService.Application.Features.Sprints.Queries.AssessRisk;
using BitirmeProject.AiService.Application.Tools;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Extensions;

namespace BitirmeProject.AiService.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public sealed class AiController : ControllerBase
{
    private readonly IMediator _mediator;

    private readonly AgentLoop _agentLoop;

    public AiController(IMediator mediator, AgentLoop agentLoop)
    {
        _mediator = mediator;
        _agentLoop = agentLoop;
    }

    private const string AgentSystemPrompt =
        "Sen BitirmeProject AI agent'ısın. Kullanıcının doğal dilde yazdığı isteği " +
        "araç çağrılarıyla gerçekleştirmek için tool catalog'unu kullanırsın. " +
        "Her adımda ya bir tool çağırırsın ya da konuşmayı 'final' ile bitirirsin. " +
        "Yalnızca geçerli JSON döndürürsün; markdown fence, açıklama, İngilizce sızıntısı yasak. " +
        "Türkçe yanıt ver. Aynı tool'u gereksiz tekrar çağırma, önce mevcut durumu sorgulamak için " +
        "get_active_sprint / get_project_issues kullan.";

    /// <summary>
    /// Generates a sprint + issue plan from a project description using the local LLM.
    /// </summary>
    [HttpPost("generate-plan")]
    public async Task<ActionResult<GeneratePlanResultDto>> GeneratePlan(
        [FromBody] GeneratePlanRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var result = await _mediator.Send(
            new GeneratePlanCommand(request.ProjectId, userId.Value, orgId, request.Description),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Enriches an issue title with description, acceptance criteria, edge cases and story point estimate.
    /// </summary>
    [HttpPost("enrich-issue")]
    public async Task<ActionResult<EnrichIssueResultDto>> EnrichIssue(
        [FromBody] EnrichIssueRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var result = await _mediator.Send(
            new EnrichIssueCommand(request.IssueId, request.Title, request.ProjectId, userId.Value, orgId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Detects potentially duplicate issues in the project for the given title.
    /// </summary>
    [HttpPost("detect-duplicate")]
    public async Task<ActionResult<DetectDuplicateResultDto>> DetectDuplicate(
        [FromBody] DetectDuplicateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var result = await _mediator.Send(
            new DetectDuplicateQuery(request.ProjectId, userId.Value, orgId, request.Title),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Generates a sprint retrospective. Can also be triggered automatically via SprintCompletedEvent.
    /// </summary>
    [HttpPost("retrospective")]
    public async Task<ActionResult<RetrospectiveResultDto>> Retrospective(
        [FromBody] RetrospectiveRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var result = await _mediator.Send(
            new GenerateRetrospectiveCommand(request.SprintId, request.ProjectId, userId.Value, orgId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Analyzes sprint workload and suggests balance improvements.
    /// </summary>
    [HttpPost("suggest-balance")]
    public async Task<ActionResult<SuggestBalanceResultDto>> SuggestBalance(
        [FromBody] SprintRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var result = await _mediator.Send(
            new SuggestBalanceCommand(request.SprintId, request.ProjectId, userId.Value, orgId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Assesses the delay risk for a sprint based on completion rate.
    /// </summary>
    [HttpPost("sprint-risk")]
    public async Task<ActionResult<SprintRiskResultDto>> SprintRisk(
        [FromBody] SprintRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var result = await _mediator.Send(
            new AssessSprintRiskQuery(request.SprintId, request.ProjectId, userId.Value, orgId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Sends a chat message to the AI with live project context (RAG-lite).
    /// Pass sessionId to continue an existing conversation, omit to start a new one.
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponseDto>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var result = await _mediator.Send(
            new SendMessageCommand(request.ProjectId, userId.Value, orgId, request.SessionId, request.Message),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Generates a full project scaffold (project + sprints + issues) from a natural-language description.
    /// Returns a draft only; the frontend orchestrates the actual creation via project/sprint/issue APIs.
    /// </summary>
    [HttpPost("scaffold-project")]
    public async Task<ActionResult<ProjectScaffoldDraftDto>> ScaffoldProject(
        [FromBody] ScaffoldRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var draft = await _mediator.Send(
            new ScaffoldProjectCommand(userId.Value, orgId, request.Description),
            cancellationToken);

        return Ok(draft);
    }

    /// <summary>
    /// Aktif Ollama modelini ve fine-tune durumunu döner. UI rozeti ve hata mesajları için kullanılır.
    /// </summary>
    [HttpGet("model-info")]
    public ActionResult<ModelInfoResponse> GetModelInfo([FromServices] IModelSelector selector)
    {
        return Ok(new ModelInfoResponse(
            Active: selector.ActiveModel,
            IsFinetuned: selector.UseFinetuned,
            BaseModel: selector.BaseModel,
            FinetunedModel: selector.FinetunedModel));
    }

    /// <summary>
    /// Runtime'da fine-tune ↔ base model arasında geçiş yapar.
    /// Demo amaçlı: tek toggle, tüm sonraki AI isteklerini etkiler.
    /// </summary>
    [HttpPost("model-mode")]
    public ActionResult<ModelInfoResponse> SetModelMode(
        [FromBody] SetModelModeRequest request,
        [FromServices] IModelSelector selector)
    {
        selector.SetUseFinetuned(request.UseFinetuned);
        return Ok(new ModelInfoResponse(
            Active: selector.ActiveModel,
            IsFinetuned: selector.UseFinetuned,
            BaseModel: selector.BaseModel,
            FinetunedModel: selector.FinetunedModel));
    }

    /// <summary>
    /// Agent endpoint — kullanıcının doğal dil isteğini tool-calling loop ile gerçekleştirir.
    /// Her tool yürütmesi AiToolExecutions tablosuna loglanır. Max 5 iter.
    /// </summary>
    [HttpPost("agent")]
    public async Task<ActionResult<AgentResponse>> Agent(
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (userId is null) return Unauthorized();

        var orgIdStr = User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdStr, out var orgId)) return Forbid();

        var context = new ToolContext(userId.Value, orgId, request.ProjectId, request.SessionId);
        var run = await _agentLoop.RunAsync(AgentSystemPrompt, request.Message, context, cancellationToken);

        return Ok(new AgentResponse(
            run.FinalText,
            run.IterationsUsed,
            run.HitIterationLimit,
            run.FormatUnrecognized,
            run.Turns.Select(t => new AgentTurnDto(t.Kind, t.Content)).ToList()));
    }
}

public sealed record GeneratePlanRequest(Guid ProjectId, string Description);
public sealed record EnrichIssueRequest(Guid IssueId, Guid ProjectId, string Title);
public sealed record DetectDuplicateRequest(Guid ProjectId, string Title);
public sealed record ChatRequest(Guid ProjectId, Guid? SessionId, string Message);
public sealed record RetrospectiveRequest(Guid SprintId, Guid ProjectId);
public sealed record SprintRequest(Guid SprintId, Guid ProjectId);
public sealed record ScaffoldRequest(string Description);
public sealed record AgentRequest(Guid ProjectId, string Message, Guid? SessionId);
public sealed record AgentResponse(
    string FinalText,
    int IterationsUsed,
    bool HitIterationLimit,
    bool FormatUnrecognized,
    IReadOnlyList<AgentTurnDto> Turns);
public sealed record AgentTurnDto(string Kind, string Content);
public sealed record ModelInfoResponse(string Active, bool IsFinetuned, string BaseModel, string FinetunedModel);
public sealed record SetModelModeRequest(bool UseFinetuned);
