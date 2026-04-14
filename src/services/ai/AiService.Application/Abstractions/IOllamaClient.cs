namespace BitirmeProject.AiService.Application.Abstractions;

public interface IOllamaClient
{
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
    Task<T?> GenerateJsonAsync<T>(string prompt, CancellationToken ct = default) where T : class;
}
