using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Tools;

public sealed record AgentTurn(string Kind, string Content);

public sealed record AgentRunResult(
    string FinalText,
    IReadOnlyList<AgentTurn> Turns,
    int IterationsUsed,
    bool HitIterationLimit);

public sealed class AgentLoop
{
    private readonly IOllamaClient _ollama;
    private readonly IToolRegistry _registry;
    private readonly IAiToolExecutionRepository _audit;
    private readonly ILogger<AgentLoop> _logger;

    private const int MaxIterations = 5;

    public AgentLoop(
        IOllamaClient ollama,
        IToolRegistry registry,
        IAiToolExecutionRepository audit,
        ILogger<AgentLoop> logger)
    {
        _ollama = ollama;
        _registry = registry;
        _audit = audit;
        _logger = logger;
    }

    public async Task<AgentRunResult> RunAsync(
        string systemPrompt,
        string userMessage,
        ToolContext context,
        CancellationToken ct = default)
    {
        var turns = new List<AgentTurn>
        {
            new("user", userMessage),
        };

        for (var i = 1; i <= MaxIterations; i++)
        {
            var prompt = BuildPrompt(systemPrompt, turns);
            var raw = await _ollama.GenerateAsync(prompt, ct);
            turns.Add(new AgentTurn("assistant", raw));

            if (!TryParse(raw, out var parsed))
            {
                _logger.LogWarning("AgentLoop iter {Iter}: model çıktısı parse edilemedi. Raw (ilk 500): {Raw}", i, raw.Length > 500 ? raw[..500] : raw);
                return new AgentRunResult(raw, turns, i, false);
            }

            if (parsed.TryGetProperty("final", out var finalEl) && finalEl.ValueKind == JsonValueKind.String)
            {
                return new AgentRunResult(finalEl.GetString() ?? string.Empty, turns, i, false);
            }

            if (!parsed.TryGetProperty("tool_calls", out var callsEl) || callsEl.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("AgentLoop iter {Iter}: ne 'final' ne 'tool_calls' alanı var", i);
                return new AgentRunResult(raw, turns, i, false);
            }

            foreach (var call in callsEl.EnumerateArray())
            {
                var name = call.TryGetProperty("name", out var nEl) && nEl.ValueKind == JsonValueKind.String
                    ? nEl.GetString()
                    : null;
                if (string.IsNullOrEmpty(name))
                {
                    turns.Add(new AgentTurn("tool", JsonSerializer.Serialize(new { error = "tool name yok" })));
                    continue;
                }

                var tool = _registry.Get(name);
                if (tool is null)
                {
                    turns.Add(new AgentTurn("tool", JsonSerializer.Serialize(new
                    {
                        name,
                        error = $"unknown tool: {name}. Catalog'da yok.",
                    })));
                    continue;
                }

                var input = call.TryGetProperty("input", out var inEl)
                    ? inEl
                    : JsonDocument.Parse("{}").RootElement;

                _logger.LogInformation("AgentLoop iter {Iter}: {Tool} çağrılıyor", i, name);

                var sw = Stopwatch.StartNew();
                var result = await tool.ExecuteAsync(context, input, ct);
                sw.Stop();

                var inputJson = input.GetRawText();
                var outputJson = result.Data is null ? null : JsonSerializer.Serialize(result.Data);

                await _audit.AddAsync(new AiToolExecution(
                    sessionId: context.SessionId,
                    userId: context.UserId,
                    organizationId: context.OrganizationId,
                    projectId: context.ProjectId,
                    toolName: name,
                    inputJson: inputJson,
                    outputJson: outputJson,
                    success: result.Success,
                    errorMessage: result.Error,
                    durationMs: sw.ElapsedMilliseconds), ct);
                await _audit.SaveChangesAsync(ct);

                turns.Add(new AgentTurn("tool", JsonSerializer.Serialize(new
                {
                    name,
                    success = result.Success,
                    data = result.Data,
                    error = result.Error,
                })));
            }
        }

        var lastAssistant = turns.LastOrDefault(t => t.Kind == "assistant")?.Content ?? string.Empty;
        return new AgentRunResult(lastAssistant, turns, MaxIterations, true);
    }

    private string BuildPrompt(string systemPrompt, IReadOnlyList<AgentTurn> turns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(systemPrompt);
        sb.AppendLine();
        sb.AppendLine("Tool catalog (kullanılabilir araçlar):");
        sb.AppendLine(_registry.GetCatalogJson().GetRawText());
        sb.AppendLine();
        sb.AppendLine("Yanıt formatı — yalnızca aşağıdaki iki JSON şemadan birinde yanıt ver. Markdown fence, açıklama metni yasak.");
        sb.AppendLine("1) Tool çağırmak için:");
        sb.AppendLine("   {\"tool_calls\": [{\"name\": \"<tool_name>\", \"input\": { ... }}]}");
        sb.AppendLine("2) Konuşmayı bitirmek için:");
        sb.AppendLine("   {\"final\": \"<kullanıcıya gönderilecek Türkçe mesaj>\"}");
        sb.AppendLine();
        sb.AppendLine("Konuşma geçmişi:");

        foreach (var t in turns)
        {
            sb.Append('[').Append(t.Kind).Append("] ");
            sb.AppendLine(t.Content);
        }

        sb.AppendLine();
        sb.Append("[assistant] ");
        return sb.ToString();
    }

    private static bool TryParse(string raw, out JsonElement result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var trimmed = raw.Trim();

        // Strip markdown fence: ```json ... ``` veya ``` ... ``` (başta/sonda, satır içinde olabilir)
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            // Açılış fence'inden sonraki ilk newline'a atla (json/diğer dil etiketini geç)
            var firstNl = trimmed.IndexOfAny(new[] { '\n', '\r' });
            if (firstNl > 0)
                trimmed = trimmed[(firstNl + 1)..];
            else
                trimmed = trimmed.TrimStart('`');

            // Kapanış fence'ini at
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
                trimmed = trimmed[..lastFence];
        }

        // Brace-balanced ilk JSON objesini ham metinden çıkar; eksik kapatıcılar varsa tahmin et
        var startIdx = trimmed.IndexOf('{');
        if (startIdx < 0) return false;

        var braceDepth = 0;
        var bracketDepth = 0;
        var inString = false;
        var escape = false;
        for (var idx = startIdx; idx < trimmed.Length; idx++)
        {
            var c = trimmed[idx];
            if (escape) { escape = false; continue; }
            if (c == '\\') { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == '{') braceDepth++;
            else if (c == '}')
            {
                braceDepth--;
                if (braceDepth == 0 && bracketDepth == 0)
                {
                    try
                    {
                        result = JsonDocument.Parse(trimmed[startIdx..(idx + 1)]).RootElement.Clone();
                        return true;
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                }
            }
            else if (c == '[') bracketDepth++;
            else if (c == ']') bracketDepth--;
        }

        // Tolerant fallback: eksik kapatıcıları (bracket/brace) sona ekleyip yeniden dene
        if (braceDepth > 0 || bracketDepth > 0)
        {
            var repaired = trimmed[startIdx..].TrimEnd('\r', '\n', ' ', ',', '`')
                + new string(']', Math.Max(0, bracketDepth))
                + new string('}', Math.Max(0, braceDepth));
            try
            {
                result = JsonDocument.Parse(repaired).RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        return false;
    }
}
