namespace BitirmeProject.Bff.Api.Models;

public sealed class ServiceEndpoints
{
    public string ProjectService { get; init; } = string.Empty;
    public string IssueService { get; init; } = string.Empty;
    public string SprintService { get; init; } = string.Empty;
    public string NotificationService { get; init; } = string.Empty;
    public string Seq { get; init; } = "http://seq:80";
}
