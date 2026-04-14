using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.Common.Mappings;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesBySprint;

public sealed class GetIssuesBySprintQueryHandler : IRequestHandler<GetIssuesBySprintQuery, IReadOnlyList<IssueDto>>
{
    private readonly IIssueRepository _repository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IMapper _mapper;

    public GetIssuesBySprintQueryHandler(IIssueRepository repository, IIssueBoardRepository boardRepository, IMapper mapper)
    {
        _repository = repository;
        _boardRepository = boardRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueDto>> Handle(GetIssuesBySprintQuery request, CancellationToken cancellationToken)
    {
        var boardItems = await _boardRepository.GetBySprintIdAsync(request.SprintId, cancellationToken);
        if (request.CallerOrgId.HasValue)
        {
            boardItems = boardItems
                .Where(item => !item.OrganizationId.HasValue || item.OrganizationId == request.CallerOrgId)
                .ToList();
        }

        var issues = await _repository.GetByIdsAsync(boardItems.Select(x => x.IssueId).ToArray(), cancellationToken);
        var issueLookup = issues.ToDictionary(x => x.Id);

        return boardItems
            .Where(boardItem => issueLookup.ContainsKey(boardItem.IssueId))
            .Select(boardItem => IssueDtoFactory.Create(issueLookup[boardItem.IssueId], boardItem.SprintId))
            .ToList();
    }
}
