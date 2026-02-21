namespace BitirmeProject.Bff.Api.Models;

public sealed class BoardConfig
{
    public IReadOnlyList<BoardColumn> Columns { get; init; } = Array.Empty<BoardColumn>();
    public IReadOnlyDictionary<string, string[]> AllowedTransitions { get; init; } = new Dictionary<string, string[]>();
}

public sealed class BoardColumn
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int? WipLimit { get; init; }
}
