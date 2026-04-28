using System.Text.Json;

namespace BitirmeProject.AiService.Application.Tools;

public interface ITool
{
    string Name { get; }

    string Description { get; }

    JsonElement InputSchema { get; }

    Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default);
}
