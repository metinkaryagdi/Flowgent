using System.Text.Json;
using BitirmeProject.AiService.Application.Abstractions;

namespace BitirmeProject.AiService.Application.Tools.Impl;

public sealed class GetActiveSprintTool : ITool
{
    private static readonly string SchemaJson =
        """
        {
          "type": "object",
          "properties": {},
          "additionalProperties": false
        }
        """;

    private readonly ISprintServiceClient _sprints;

    public GetActiveSprintTool(ISprintServiceClient sprints)
    {
        _sprints = sprints;
    }

    public string Name => "get_active_sprint";

    public string Description =>
        "Context'teki projenin aktif sprint'ini döner (yoksa null). " +
        "Yeni issue'ları hangi sprint'e bağlayacağını belirlemek için çağır.";

    public JsonElement InputSchema { get; } = ToolSchemas.Parse(SchemaJson);

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        try
        {
            var active = await _sprints.GetActiveSprintAsync(context.ProjectId, context.OrganizationId, ct);
            return ToolResult.Ok(active);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"get_active_sprint başarısız: {ex.Message}");
        }
    }
}
