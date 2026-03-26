using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProjectPaged;

public sealed class GetIssuesByProjectPagedQueryHandler : IRequestHandler<GetIssuesByProjectPagedQuery, PagedResult<IssueBoardItemDto>>
{
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IMapper _mapper;

    public GetIssuesByProjectPagedQueryHandler(IIssueBoardRepository boardRepository, IMapper mapper)
    {
        _boardRepository = boardRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<IssueBoardItemDto>> Handle(GetIssuesByProjectPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _boardRepository.GetByProjectIdPagedAsync(
            request.ProjectId,
            request.Page,
            request.PageSize,
            request.SprintId,
            request.BacklogOnly,
            cancellationToken);

        return new PagedResult<IssueBoardItemDto>
        {
            Items = items.Select(item => _mapper.Map<IssueBoardItemDto>(item)).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
