using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.AiService.Application.Tools;

public sealed record AgentTurn(string Kind, string Content);

public sealed record AgentRunResult(
    string FinalText,
    IReadOnlyList<AgentTurn> Turns,
    int IterationsUsed,
    bool HitIterationLimit,
    bool FormatUnrecognized = false);

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
        // Training dataset'iyle bire bir uyumlu messages listesi inşa ediliyor.
        // System prompt = AgentSystemPrompt + tool catalog + format spec (agent.py system_prompt() ile aynı yapı).
        // Multi-turn: system → user → assistant tool_calls → user [tool] result → assistant final
        var messages = new List<ChatMessage>
        {
            new("system", BuildSystemContent(systemPrompt)),
            new("user", userMessage),
        };

        // UI/audit için kind-based turns paralel olarak tutuluyor (mevcut UI sözleşmesi korunsun)
        var turns = new List<AgentTurn>
        {
            new("user", userMessage),
        };

        for (var i = 1; i <= MaxIterations; i++)
        {
            var raw = await _ollama.ChatJsonRawAsync(messages, ct);
            messages.Add(new ChatMessage("assistant", raw));
            turns.Add(new AgentTurn("assistant", raw));

            if (!TryParse(raw, out var parsed))
            {
                _logger.LogWarning("AgentLoop iter {Iter}: model çıktısı parse edilemedi. Raw (ilk 500): {Raw}", i, raw.Length > 500 ? raw[..500] : raw);
                return new AgentRunResult(raw, turns, i, false, FormatUnrecognized: true);
            }

            if (TryGetFinal(parsed, out var finalText))
            {
                return new AgentRunResult(finalText, turns, i, false);
            }

            if (!TryGetToolCalls(parsed, out var callsEl))
            {
                if (TryGetFinalFallback(parsed, out var fallbackFinal))
                {
                    return new AgentRunResult(fallbackFinal, turns, i, false);
                }
                _logger.LogWarning("AgentLoop iter {Iter}: ne 'final' ne 'tool_calls' alanı var. Tanınan anahtarlar yok.", i);
                return new AgentRunResult(raw, turns, i, false, FormatUnrecognized: true);
            }

            foreach (var call in callsEl.EnumerateArray())
            {
                var name = TryGetToolName(call);
                if (string.IsNullOrEmpty(name))
                {
                    var err = JsonSerializer.Serialize(new { error = "tool name yok" });
                    turns.Add(new AgentTurn("tool", err));
                    messages.Add(new ChatMessage("user", "[tool] " + err));
                    continue;
                }

                var tool = _registry.Get(name);
                if (tool is null)
                {
                    var err = JsonSerializer.Serialize(new
                    {
                        name,
                        error = $"unknown tool: {name}. Catalog'da yok.",
                    });
                    turns.Add(new AgentTurn("tool", err));
                    messages.Add(new ChatMessage("user", "[tool] " + err));
                    continue;
                }

                var input = TryGetToolInput(call);
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

                // Training format ile birebir: tool result payload + "[tool] " prefix taşıyan user-role mesajı
                var toolPayload = JsonSerializer.Serialize(
                    new { name, success = result.Success, data = result.Data, error = result.Error },
                    s_toolResultOptions);
                turns.Add(new AgentTurn("tool", toolPayload));
                messages.Add(new ChatMessage("user", "[tool] " + toolPayload));
            }
        }

        var lastAssistant = turns.LastOrDefault(t => t.Kind == "assistant")?.Content ?? string.Empty;
        return new AgentRunResult(lastAssistant, turns, MaxIterations, true);
    }

    private static readonly JsonSerializerOptions s_toolResultOptions = new()
    {
        // Tool result JSON'u training data ile aynı format'ta üretilsin (null değerler yazılsın, alan isimleri lowercase)
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Eğitim setindeki system prompt ile aynı yapıyı üretir:
    /// {systemPrompt}\n\nTool catalog (kullanılabilir araçlar):\n{catalog}\n\n{format_spec}
    /// (tools/ai_data_collector/prompts/agent.py system_prompt() ile eşleşmeli)
    /// </summary>
    private string BuildSystemContent(string systemPrompt)
    {
        var sb = new StringBuilder();
        sb.Append(systemPrompt);
        sb.Append("\n\nTool catalog (kullanılabilir araçlar):\n");
        sb.Append(_registry.GetCatalogJson().GetRawText());
        sb.Append("\n\n");
        sb.Append("Yanıt formatı — yalnızca aşağıdaki iki JSON şemadan birinde yanıt ver. Markdown fence, açıklama metni yasak.\n");
        sb.Append("1) Tool çağırmak için:\n");
        sb.Append("   {\"tool_calls\": [{\"name\": \"<tool_name>\", \"input\": { ... }}]}\n");
        sb.Append("2) Konuşmayı bitirmek için:\n");
        sb.Append("   {\"final\": \"<kullanıcıya gönderilecek Türkçe mesaj>\"}");
        return sb.ToString();
    }

    // Fine-tune modelinin Türkçe'ye çevirdiği şema anahtarlarını da kabul et.
    // bp-agent r=8 / 125 step ile "tool_calls" → "lista", "final" → "cevap" gibi sapmalar yapıyor.
    private static readonly string[] FinalAliases = ["final", "cevap", "yanit", "yanıt", "sonuc", "sonuç", "mesaj", "answer"];
    private static readonly string[] ToolCallsAliases = ["tool_calls", "toolCalls", "lista", "liste", "araclar", "araçlar", "tools", "calls", "cagrilar", "çağrılar", "tool_call"];
    private static readonly string[] ToolNameAliases = ["name", "isim", "ad", "arac", "araç", "tool"];
    private static readonly string[] ToolInputAliases = ["input", "girdi", "args", "arguments", "parametreler", "params"];

    private static bool TryGetFinal(JsonElement parsed, out string finalText)
    {
        foreach (var key in FinalAliases)
        {
            if (parsed.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
            {
                finalText = el.GetString() ?? string.Empty;
                return true;
            }
        }
        finalText = string.Empty;
        return false;
    }

    // Yapısal final fallback: alias eşleşmediyse ve obje sadece tek bir string property taşıyorsa
    // (ör. {"z": "Aktif sprint X'tir"}) — onu final mesaj say. Tool çağrılarıyla karıştırmamak için
    // sadece tek property ve string değeri varsa kabul et.
    private static bool TryGetFinalFallback(JsonElement parsed, out string finalText)
    {
        finalText = string.Empty;
        if (parsed.ValueKind != JsonValueKind.Object) return false;

        string? candidate = null;
        var count = 0;
        foreach (var prop in parsed.EnumerateObject())
        {
            count++;
            if (count > 1) return false;
            if (prop.Value.ValueKind != JsonValueKind.String) return false;
            candidate = prop.Value.GetString();
        }

        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length < 5) return false;
        finalText = candidate;
        return true;
    }

    private static bool TryGetToolCalls(JsonElement parsed, out JsonElement callsEl)
    {
        foreach (var key in ToolCallsAliases)
        {
            if (parsed.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.Array)
            {
                callsEl = el;
                return true;
            }
        }

        // Yapısal fallback: fine-tune model rastgele anahtar (yc, lista, …) üretebilir.
        // En üstte herhangi bir array property varsa ve içindeki ilk öğe {name|isim|…} taşıyorsa onu tool_calls say.
        if (parsed.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in parsed.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array) continue;
                var arr = prop.Value;
                if (arr.GetArrayLength() == 0) continue;
                var first = arr[0];
                if (first.ValueKind != JsonValueKind.Object) continue;
                if (TryGetToolName(first) is not null)
                {
                    callsEl = arr;
                    return true;
                }
            }
        }

        callsEl = default;
        return false;
    }

    private static string? TryGetToolName(JsonElement call)
    {
        foreach (var key in ToolNameAliases)
        {
            if (call.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        }
        return null;
    }

    private static JsonElement TryGetToolInput(JsonElement call)
    {
        foreach (var key in ToolInputAliases)
        {
            if (call.TryGetProperty(key, out var el))
                return el;
        }
        return JsonDocument.Parse("{}").RootElement;
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
