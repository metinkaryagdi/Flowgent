namespace BitirmeProject.Bff.Api.Models;

public sealed class SprintDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Goal { get; init; }
    public int Status { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
