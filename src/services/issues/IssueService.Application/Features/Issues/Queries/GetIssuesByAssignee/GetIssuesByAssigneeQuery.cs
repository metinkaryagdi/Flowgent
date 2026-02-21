using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByAssignee;

public sealed record GetIssuesByAssigneeQuery(Guid AssigneeUserId) : IRequest<IReadOnlyList<IssueDto>>;
