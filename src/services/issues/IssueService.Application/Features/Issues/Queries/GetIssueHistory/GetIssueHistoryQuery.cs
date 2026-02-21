using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueHistory;

public sealed record GetIssueHistoryQuery(Guid IssueId) : IRequest<IReadOnlyList<IssueAuditDto>>;
