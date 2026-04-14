namespace BitirmeProject.AiService.Application.DTOs;

public sealed class RetrospectiveResultDto
{
    public Guid SessionId { get; init; }
    public Guid SprintId { get; init; }
    public string Summary { get; init; } = null!;
    public string WentWell { get; init; } = null!;
    public string Improvements { get; init; } = null!;
    public string ActionItems { get; init; } = null!;
}

public sealed class SuggestBalanceResultDto
{
    public Guid SessionId { get; init; }
    public Guid SprintId { get; init; }
    public string Analysis { get; init; } = null!;
    public string Recommendation { get; init; } = null!;
    public List<BalanceSuggestionItemDto> Suggestions { get; init; } = new();
}

public sealed class BalanceSuggestionItemDto
{
    public string IssueTitle { get; init; } = null!;
    public string CurrentPriority { get; init; } = null!;
    public string SuggestedAction { get; init; } = null!;
}

public sealed class SprintRiskResultDto
{
    public Guid SessionId { get; init; }
    public Guid SprintId { get; init; }
    public string RiskLevel { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public string Recommendation { get; init; } = null!;
    public int TotalIssues { get; init; }
    public int DoneIssues { get; init; }
    public int InProgressIssues { get; init; }
    public int OpenIssues { get; init; }
}
