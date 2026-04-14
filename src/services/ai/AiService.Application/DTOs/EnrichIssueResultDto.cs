namespace BitirmeProject.AiService.Application.DTOs;

public sealed class EnrichIssueResultDto
{
    public Guid SessionId { get; init; }
    public string Description { get; init; } = null!;
    public string AcceptanceCriteria { get; init; } = null!;
    public string EdgeCases { get; init; } = null!;
    public int StoryPoints { get; init; }
}

public sealed class DetectDuplicateResultDto
{
    public Guid SessionId { get; init; }
    public List<SimilarIssueDto> SimilarIssues { get; init; } = new();
}

public sealed class SimilarIssueDto
{
    public Guid IssueId { get; init; }
    public string Title { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public int SimilarityScore { get; init; }
}

public sealed class ProjectIssueDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
}
