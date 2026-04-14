namespace BitirmeProject.AiService.Application.DTOs;

public sealed class AiSessionDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Type { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
