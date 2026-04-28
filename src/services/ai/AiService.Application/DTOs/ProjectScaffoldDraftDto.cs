namespace BitirmeProject.AiService.Application.DTOs;

/// <summary>
/// AI-üretilmiş proje taslağı. Backend DB'ye dokunmaz; frontend onayladıktan sonra
/// kullanıcı JWT'siyle project/sprint/issue endpoint'lerine yazar.
/// </summary>
public sealed class ProjectScaffoldDraftDto
{
    public Guid SessionId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string ProjectKey { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public List<DraftSprintDto> Sprints { get; set; } = new();
}

public sealed class DraftSprintDto
{
    public string Name { get; set; } = null!;
    public string Goal { get; set; } = string.Empty;
    public List<DraftIssueDto> Issues { get; set; } = new();
}

public sealed class DraftIssueDto
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
}
