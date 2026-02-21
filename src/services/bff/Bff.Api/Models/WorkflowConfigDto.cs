namespace BitirmeProject.Bff.Api.Models;

public sealed class WorkflowConfigDto
{
    public IReadOnlyList<string> Statuses { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string[]> AllowedTransitions { get; init; } = new Dictionary<string, string[]>();
}
