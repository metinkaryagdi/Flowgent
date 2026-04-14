namespace BitirmeProject.AiService.Infrastructure.Options;

public sealed class InternalServiceOptions
{
    public string CallerName { get; init; } = "AiService";
    public string ApiKey { get; init; } = string.Empty;
}
