using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProjectPaged;

public sealed record GetIssuesByProjectPagedQuery(
    Guid ProjectId,
    int Page,
    int PageSize,
    Guid? SprintId,
    bool BacklogOnly) : IRequest<PagedResult<IssueBoardItemDto>>;
