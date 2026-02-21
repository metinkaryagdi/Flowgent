using BitirmeProject.SprintService.Domain.Enums;

namespace BitirmeProject.SprintService.Application.DTOs;

public sealed class SprintDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Goal { get; init; }
    public SprintStatus Status { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
