using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProject;

public sealed class GetIssuesByProjectQueryHandler : IRequestHandler<GetIssuesByProjectQuery, IReadOnlyList<IssueBoardItemDto>>
{
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IMapper _mapper;

    public GetIssuesByProjectQueryHandler(IIssueBoardRepository boardRepository, IMapper mapper)
    {
        _boardRepository = boardRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueBoardItemDto>> Handle(GetIssuesByProjectQuery request, CancellationToken cancellationToken)
    {
        var items = await _boardRepository.GetByProjectIdAsync(request.ProjectId, request.CallerOrgId, cancellationToken);
        return items.Select(item => _mapper.Map<IssueBoardItemDto>(item)).ToList();
    }
}
