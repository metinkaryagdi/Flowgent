using BitirmeProject.AiService.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BitirmeProject.AiService.Infrastructure.Clients;

public sealed class OllamaClient : IOllamaClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _fallbackModel;
    private readonly ILogger<OllamaClient> _logger;
    private static readonly SemaphoreSlim _concurrencyLimit = new(5, 5);
    private const int MaxRetries = 3;

    public OllamaClient(HttpClient http, IConfiguration configuration, ILogger<OllamaClient> logger)
    {
        _http = http;
        _model = configuration["Ollama:Model"] ?? "gemma3:4b";
        _fallbackModel = configuration["Ollama:FallbackModel"] ?? "llama3.2:3b";
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        return await GenerateWithRetryAsync(prompt, _model, ct);
    }

    public async Task<T?> GenerateJsonAsync<T>(string prompt, CancellationToken ct = default) where T : class
    {
        var raw = await GenerateWithRetryAsync(prompt, _model, ct);

        var parsed = TryParseJson<T>(raw);
        if (parsed is not null)
            return parsed;

        _logger.LogWarning("Primary model JSON parse failed, retrying with fallback model {Model}", _fallbackModel);
        var rawFallback = await GenerateWithRetryAsync(prompt, _fallbackModel, ct);
        return TryParseJson<T>(rawFallback);
    }

    private async Task<string> GenerateWithRetryAsync(string prompt, string model, CancellationToken ct)
    {
        await _concurrencyLimit.WaitAsync(ct);
        try
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    return await GenerateWithModelAsync(prompt, model, ct);
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning(ex, "Ollama attempt {Attempt}/{Max} failed, retrying in {Delay}s", attempt, MaxRetries, delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                }
            }
            return await GenerateWithModelAsync(prompt, model, ct);
        }
        finally
        {
            _concurrencyLimit.Release();
        }
    }

    private async Task<string> GenerateWithModelAsync(string prompt, string model, CancellationToken ct)
    {
        var payload = new
        {
            model,
            prompt,
            stream = false
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
        return result?.Response ?? string.Empty;
    }

    private static T? TryParseJson<T>(string raw) where T : class
    {
        // Strip markdown code fences if present
        var json = Regex.Replace(raw.Trim(), @"^```(?:json)?\s*|\s*```$", string.Empty, RegexOptions.Multiline).Trim();

        // Find first { ... } block
        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start >= 0 && end > start)
            json = json[start..(end + 1)];

        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private sealed class OllamaGenerateResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}
