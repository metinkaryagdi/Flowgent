using System.Text.Json.Serialization;

namespace BitirmeProject.AiService.Application.DTOs;

/// <summary>
/// JSON structure Ollama is expected to return for plan generation.
/// </summary>
public sealed class OllamaPlanResponse
{
    [JsonPropertyName("sprints")]
    public List<SprintPlanDto> Sprints { get; set; } = new();
}

public sealed class SprintPlanDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("issues")]
    public List<IssuePlanDto> Issues { get; set; } = new();
}

public sealed class IssuePlanDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "Medium";

    [JsonPropertyName("storyPoints")]
    public int StoryPoints { get; set; } = 3;
}
