using System.Collections.ObjectModel;
using System.Text.Json;

namespace BitirmeProject.AiService.Application.Tools;

public interface IToolRegistry
{
    IReadOnlyCollection<ITool> All { get; }

    ITool? Get(string name);

    JsonElement GetCatalogJson();
}

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;
    private readonly ReadOnlyCollection<ITool> _all;
    private readonly Lazy<JsonElement> _catalog;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _all = new ReadOnlyCollection<ITool>(_tools.Values.ToList());
        _catalog = new Lazy<JsonElement>(BuildCatalog);
    }

    public IReadOnlyCollection<ITool> All => _all;

    public ITool? Get(string name) =>
        _tools.TryGetValue(name, out var t) ? t : null;

    public JsonElement GetCatalogJson() => _catalog.Value;

    private JsonElement BuildCatalog()
    {
        var items = _all.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = t.InputSchema,
        }).ToArray();

        var json = JsonSerializer.Serialize(items);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
