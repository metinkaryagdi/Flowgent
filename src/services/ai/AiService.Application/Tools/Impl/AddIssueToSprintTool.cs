using System.Text.Json;
using BitirmeProject.AiService.Application.Abstractions;

namespace BitirmeProject.AiService.Application.Tools.Impl;

public sealed class AddIssueToSprintTool : ITool
{
    private static readonly string SchemaJson =
        """
        {
          "type": "object",
          "properties": {
            "sprintId": { "type": "string", "format": "uuid" },
            "issueId":  { "type": "string", "format": "uuid" }
          },
          "required": ["sprintId", "issueId"],
          "additionalProperties": false
        }
        """;

    private readonly ISprintServiceClient _sprints;

    public AddIssueToSprintTool(ISprintServiceClient sprints)
    {
        _sprints = sprints;
    }

    public string Name => "add_issue_to_sprint";

    public string Description =>
        "Mevcut bir issue'yu mevcut bir sprint'e bağlar. Her ikisi de aynı projeye ait olmalı.";

    public JsonElement InputSchema { get; } = ToolSchemas.Parse(SchemaJson);

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        if (!TryParseGuid(input, "sprintId", out var sprintId))
            return ToolResult.Fail("sprintId geçerli UUID olmalı.");
        if (!TryParseGuid(input, "issueId", out var issueId))
            return ToolResult.Fail("issueId geçerli UUID olmalı.");

        try
        {
            await _sprints.AddIssueToSprintAsync(sprintId, issueId, context.UserId, ct);
            return ToolResult.Ok(new { sprintId, issueId, attached = true });
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"add_issue_to_sprint başarısız: {ex.Message}");
        }
    }

    private static bool TryParseGuid(JsonElement input, string key, out Guid value)
    {
        value = Guid.Empty;
        return input.TryGetProperty(key, out var el)
               && el.ValueKind == JsonValueKind.String
               && Guid.TryParse(el.GetString(), out value);
    }
}
