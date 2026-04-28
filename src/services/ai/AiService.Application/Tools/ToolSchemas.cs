using System.Text.Json;

namespace BitirmeProject.AiService.Application.Tools;

internal static class ToolSchemas
{
    public static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();
}
