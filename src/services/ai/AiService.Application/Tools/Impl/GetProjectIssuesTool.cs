using System.Text.Json;
using BitirmeProject.AiService.Application.Abstractions;

namespace BitirmeProject.AiService.Application.Tools.Impl;

public sealed class GetProjectIssuesTool : ITool
{
    private static readonly string SchemaJson =
        """
        {
          "type": "object",
          "properties": {},
          "additionalProperties": false
        }
        """;

    private readonly IIssueServiceClient _issues;

    public GetProjectIssuesTool(IIssueServiceClient issues)
    {
        _issues = issues;
    }

    public string Name => "get_project_issues";

    public string Description =>
        "Context'teki projenin issue listesini döner. Aynı başlıklı issue zaten var mı kontrolü veya duplikasyon önleme için kullan.";

    public JsonElement InputSchema { get; } = ToolSchemas.Parse(SchemaJson);

    public async Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default)
    {
        try
        {
            var issues = await _issues.GetIssuesByProjectAsync(context.ProjectId, context.OrganizationId, ct);
            return ToolResult.Ok(issues);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"get_project_issues başarısız: {ex.Message}");
        }
    }
}
