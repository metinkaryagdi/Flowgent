using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProject;

public sealed record GetIssuesByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<IssueBoardItemDto>>;
