using System.Text.Json.Serialization;

namespace BitirmeProject.AiService.Application.DTOs;

public sealed class GeneratePlanResultDto
{
    public Guid SessionId { get; init; }
    public List<CreatedSprintDto> Sprints { get; init; } = new();
}

public sealed class CreatedSprintDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Goal { get; init; } = null!;
    public List<CreatedIssueDto> Issues { get; init; } = new();
}

public sealed class CreatedIssueDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Priority { get; init; } = null!;
}

public sealed class ActiveSprintDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Goal { get; init; }
    public List<ActiveSprintIssueDto> Issues { get; init; } = new();
}

public sealed class ActiveSprintIssueDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string Priority { get; init; } = null!;
    [JsonPropertyName("assigneeName")]
    public string? AssigneeName { get; init; }
}

public sealed class SprintDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Goal { get; init; }
    public string Status { get; init; } = null!;
    public List<SprintDetailIssueDto> Issues { get; init; } = new();
}

public sealed class SprintDetailIssueDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string Priority { get; init; } = null!;
}
