namespace BitirmeProject.AiService.Application.Tools;

public sealed record ToolResult(bool Success, object? Data, string? Error)
{
    public static ToolResult Ok(object? data) => new(true, data, null);

    public static ToolResult Fail(string error) => new(false, null, error);
}
