using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.Common.Mappings;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByAssignee;

public sealed class GetIssuesByAssigneeQueryHandler : IRequestHandler<GetIssuesByAssigneeQuery, IReadOnlyList<IssueDto>>
{
    private readonly IIssueRepository _repository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IMapper _mapper;

    public GetIssuesByAssigneeQueryHandler(IIssueRepository repository, IIssueBoardRepository boardRepository, IMapper mapper)
    {
        _repository = repository;
        _boardRepository = boardRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueDto>> Handle(GetIssuesByAssigneeQuery request, CancellationToken cancellationToken)
    {
        var issues = await _repository.GetByAssigneeAsync(request.AssigneeUserId, cancellationToken);
        if (request.CallerOrgId.HasValue)
        {
            issues = issues
                .Where(issue => !issue.OrganizationId.HasValue || issue.OrganizationId == request.CallerOrgId)
                .ToList();
        }

        var boardItems = await _boardRepository.GetByIssueIdsAsync(issues.Select(x => x.Id).ToArray(), cancellationToken);
        var sprintLookup = boardItems.ToDictionary(x => x.IssueId, x => x.SprintId);

        return issues
            .Select(issue => IssueDtoFactory.Create(
                issue,
                sprintLookup.TryGetValue(issue.Id, out var sprintId) ? sprintId : null))
            .ToList();
    }
}
