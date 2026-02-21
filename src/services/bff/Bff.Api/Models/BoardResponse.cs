namespace BitirmeProject.Bff.Api.Models;

public sealed class BoardResponse
{
    public ProjectDto? Project { get; init; }
    public BoardConfig Config { get; init; } = new();
    public IReadOnlyList<IssueBoardItemDto> Items { get; init; } = Array.Empty<IssueBoardItemDto>();
}
