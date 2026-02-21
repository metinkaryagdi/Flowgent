using BitirmeProject.IssueService.Domain.Enums;

namespace BitirmeProject.IssueService.Domain.Workflow;

public static class IssueWorkflow
{
    public static readonly IReadOnlyDictionary<IssueStatus, IssueStatus[]> AllowedTransitions = new Dictionary<IssueStatus, IssueStatus[]>
    {
        { IssueStatus.Open, new[] { IssueStatus.InProgress } },
        { IssueStatus.InProgress, new[] { IssueStatus.Done } },
        { IssueStatus.Done, new[] { IssueStatus.InProgress } }
    };

    public static bool IsTransitionAllowed(IssueStatus from, IssueStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static IReadOnlyList<IssueStatus> Statuses => Enum.GetValues<IssueStatus>();
}
