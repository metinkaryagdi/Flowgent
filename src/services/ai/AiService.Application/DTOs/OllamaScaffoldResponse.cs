using System.Text.Json.Serialization;

namespace BitirmeProject.AiService.Application.DTOs;

/// <summary>
/// JSON structure expected from the LLM for project scaffolding.
/// </summary>
public sealed class OllamaScaffoldResponse
{
    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = null!;

    [JsonPropertyName("projectKey")]
    public string ProjectKey { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("sprints")]
    public List<ScaffoldSprintDto> Sprints { get; set; } = new();
}

public sealed class ScaffoldSprintDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("issues")]
    public List<ScaffoldIssueDto> Issues { get; set; } = new();
}

public sealed class ScaffoldIssueDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "Medium";
}
