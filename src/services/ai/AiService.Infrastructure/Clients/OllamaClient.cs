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
        var baseModel = configuration["Ollama:Model"] ?? "gemma3:4b";
        var useFinetuned = configuration.GetValue("Ollama:UseFinetuned", false);
        var finetunedModel = configuration["Ollama:FinetunedModel"] ?? "bp-agent";
        _model = useFinetuned ? finetunedModel : baseModel;
        _fallbackModel = configuration["Ollama:FallbackModel"] ?? "llama3.2:3b";
        _logger = logger;

        if (useFinetuned)
            logger.LogInformation("Ollama using fine-tuned model {Model} (base: {Base})", _model, baseModel);
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        return await GenerateWithRetryAsync(prompt, _model, jsonFormat: false, ct);
    }

    public async Task<T?> GenerateJsonAsync<T>(string prompt, CancellationToken ct = default) where T : class
    {
        var raw = await GenerateWithRetryAsync(prompt, _model, jsonFormat: true, ct);

        var parsed = TryParseJson<T>(raw);
        if (parsed is not null)
            return parsed;

        _logger.LogWarning("Primary model JSON parse failed (raw len={Len}), retrying with fallback model {Model}. Raw start: {Snippet}",
            raw.Length, _fallbackModel, raw.Length > 200 ? raw[..200] : raw);
        var rawFallback = await GenerateWithRetryAsync(prompt, _fallbackModel, jsonFormat: true, ct);
        var fallbackParsed = TryParseJson<T>(rawFallback);
        if (fallbackParsed is null)
            _logger.LogWarning("Fallback model JSON parse also failed (raw len={Len}). Raw start: {Snippet}",
                rawFallback.Length, rawFallback.Length > 200 ? rawFallback[..200] : rawFallback);
        return fallbackParsed;
    }

    private async Task<string> GenerateWithRetryAsync(string prompt, string model, bool jsonFormat, CancellationToken ct)
    {
        await _concurrencyLimit.WaitAsync(ct);
        try
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    return await GenerateWithModelAsync(prompt, model, jsonFormat, ct);
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning(ex, "Ollama attempt {Attempt}/{Max} failed, retrying in {Delay}s", attempt, MaxRetries, delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                }
            }
            return await GenerateWithModelAsync(prompt, model, jsonFormat, ct);
        }
        finally
        {
            _concurrencyLimit.Release();
        }
    }

    private async Task<string> GenerateWithModelAsync(string prompt, string model, bool jsonFormat, CancellationToken ct)
    {
        object payload = jsonFormat
            ? new { model, prompt, stream = false, format = "json" }
            : new { model, prompt, stream = false };

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
