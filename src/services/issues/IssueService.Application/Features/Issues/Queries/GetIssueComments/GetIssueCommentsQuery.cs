using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueComments;

public sealed record GetIssueCommentsQuery(Guid IssueId) : IRequest<IReadOnlyList<IssueCommentDto>>;
