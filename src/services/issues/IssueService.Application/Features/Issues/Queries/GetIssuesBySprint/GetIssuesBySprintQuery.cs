using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesBySprint;

public sealed record GetIssuesBySprintQuery(Guid SprintId) : IRequest<IReadOnlyList<IssueDto>>;
