namespace BitirmeProject.AiService.Application.Abstractions;

/// <summary>
/// Chat-format mesaj — Ollama /api/chat endpoint'i ile training dataset'inin
/// messages formatı arasında 1:1 eşleşme sağlar.
/// </summary>
/// <param name="Role">"system" | "user" | "assistant"</param>
/// <param name="Content">Mesaj içeriği (assistant turn'leri JSON object string olur)</param>
public sealed record ChatMessage(string Role, string Content);

public interface IOllamaClient
{
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateJsonRawAsync(string prompt, CancellationToken ct = default);
    Task<T?> GenerateJsonAsync<T>(string prompt, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Multi-turn chat çağrısı (Ollama /api/chat). Chat template otomatik uygulanır,
    /// böylece messages formatında eğitilen fine-tune modelleriyle uyumlu olur.
    /// `format: "json"` enforced → assistant content her zaman geçerli JSON döner.
    /// </summary>
    Task<string> ChatJsonRawAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default);
}
