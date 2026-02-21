using BitirmeProject.IssueService.Domain.Enums;

namespace BitirmeProject.IssueService.Application.DTOs;

public sealed class IssueAuditDto
{
    public Guid IssueId { get; init; }
    public IssueStatus FromStatus { get; init; }
    public IssueStatus ToStatus { get; init; }
    public Guid ChangedByUserId { get; init; }
    public DateTime ChangedAt { get; init; }
}
