namespace BitirmeProject.Bff.Api.Models;

public sealed class UiFlags
{
    public bool CanManageProjects { get; init; }
    public bool CanEditIssues { get; init; }
    public bool CanAssignIssues { get; init; }
    public bool CanChangeStatus { get; init; }
    public bool CanViewAdmin { get; init; }
}
