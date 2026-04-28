using System.Text.Json;
using BitirmeProject.AiService.Application.Abstractions;

namespace BitirmeProject.AiService.Application.Tools.Impl;

public sealed class CreateSprintTool : ITool
{
    private static readonly string SchemaJson =
        """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string", "minLength": 3,  "maxLength": 120 },
            "goal": { "type": "string", "minLength": 5,  "maxLength": 300 }
          },
          "required": ["name", "goal"],
          "additionalProperties": false
        }
        """;

    private readonly ISprintServiceClient _sprints;

    public CreateSprintTool(ISprintServiceClient sprints)
    {
        _sprints = sprints;
    }

    public string Name => "create_sprint";

    public string Description =>
        "Projeye yeni bir sprint oluşturur. 'Sprint N: Tema' formatında name + tek cümle goal zorunlu.";

    public JsonElement InputSchema { get; } = ToolSchemas.Parse(SchemaJson);

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        if (!input.TryGetProperty("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
            return ToolResult.Fail("name zorunlu string alanı.");
        if (!input.TryGetProperty("goal", out var goalEl) || goalEl.ValueKind != JsonValueKind.String)
            return ToolResult.Fail("goal zorunlu string alanı.");

        try
        {
            var created = await _sprints.CreateSprintAsync(
                context.ProjectId, context.UserId, context.OrganizationId,
                nameEl.GetString()!, goalEl.GetString()!, ct);
            return ToolResult.Ok(created);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"create_sprint başarısız: {ex.Message}");
        }
    }
}
