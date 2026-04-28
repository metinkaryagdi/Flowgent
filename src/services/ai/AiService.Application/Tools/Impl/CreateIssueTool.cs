using System.Text.Json;
using BitirmeProject.AiService.Application.Abstractions;

namespace BitirmeProject.AiService.Application.Tools.Impl;

public sealed class CreateIssueTool : ITool
{
    private static readonly string SchemaJson =
        """
        {
          "type": "object",
          "properties": {
            "title":       { "type": "string", "minLength": 3,  "maxLength": 120 },
            "description": { "type": "string", "maxLength": 2000 },
            "priority":    { "type": "string", "enum": ["Low", "Medium", "High", "Critical"] }
          },
          "required": ["title", "priority"],
          "additionalProperties": false
        }
        """;

    private readonly IIssueServiceClient _issues;

    public CreateIssueTool(IIssueServiceClient issues)
    {
        _issues = issues;
    }

    public string Name => "create_issue";

    public string Description =>
        "Projeye yeni bir issue oluşturur. title + priority zorunlu. " +
        "Mevcut organizasyonun mevcut projesine bağlanır (organizationId ve projectId context'ten gelir).";

    public JsonElement InputSchema { get; } = ToolSchemas.Parse(SchemaJson);

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        if (!input.TryGetProperty("title", out var titleEl) || titleEl.ValueKind != JsonValueKind.String)
            return ToolResult.Fail("title zorunlu string alanı.");
        if (!input.TryGetProperty("priority", out var prEl) || prEl.ValueKind != JsonValueKind.String)
            return ToolResult.Fail("priority zorunlu string alanı.");

        var title = titleEl.GetString()!;
        var priority = prEl.GetString()!;
        var description = input.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
            ? descEl.GetString()
            : null;

        try
        {
            var created = await _issues.CreateIssueAsync(
                context.ProjectId, context.UserId, context.OrganizationId,
                title, description, priority, ct);
            return ToolResult.Ok(created);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"create_issue başarısız: {ex.Message}");
        }
    }
}
