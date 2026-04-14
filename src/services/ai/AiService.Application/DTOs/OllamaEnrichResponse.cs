using System.Text.Json.Serialization;

namespace BitirmeProject.AiService.Application.DTOs;

public sealed class OllamaEnrichResponse
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("acceptanceCriteria")]
    public string AcceptanceCriteria { get; set; } = string.Empty;

    [JsonPropertyName("edgeCases")]
    public string EdgeCases { get; set; } = string.Empty;

    [JsonPropertyName("storyPoints")]
    public int StoryPoints { get; set; } = 3;
}

public sealed class OllamaDuplicateResponse
{
    [JsonPropertyName("similarIssues")]
    public List<OllamaSimilarIssue> SimilarIssues { get; set; } = new();
}

public sealed class OllamaSimilarIssue
{
    [JsonPropertyName("issueId")]
    public string IssueId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("similarityScore")]
    public int SimilarityScore { get; set; }
}

public sealed class OllamaRetrospectiveResponse
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("wentWell")]
    public string WentWell { get; set; } = string.Empty;

    [JsonPropertyName("improvements")]
    public string Improvements { get; set; } = string.Empty;

    [JsonPropertyName("actionItems")]
    public string ActionItems { get; set; } = string.Empty;
}

public sealed class OllamaBalanceResponse
{
    [JsonPropertyName("analysis")]
    public string Analysis { get; set; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;

    [JsonPropertyName("suggestions")]
    public List<OllamaBalanceSuggestion> Suggestions { get; set; } = new();
}

public sealed class OllamaBalanceSuggestion
{
    [JsonPropertyName("issueTitle")]
    public string IssueTitle { get; set; } = string.Empty;

    [JsonPropertyName("currentPriority")]
    public string CurrentPriority { get; set; } = string.Empty;

    [JsonPropertyName("suggestedAction")]
    public string SuggestedAction { get; set; } = string.Empty;
}

public sealed class OllamaRiskResponse
{
    [JsonPropertyName("riskLevel")]
    public string RiskLevel { get; set; } = "Medium";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;
}
