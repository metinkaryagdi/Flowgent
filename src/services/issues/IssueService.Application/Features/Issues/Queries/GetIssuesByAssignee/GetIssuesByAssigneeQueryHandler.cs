using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByAssignee;

public sealed class GetIssuesByAssigneeQueryHandler : IRequestHandler<GetIssuesByAssigneeQuery, IReadOnlyList<IssueDto>>
{
    private readonly IIssueRepository _repository;
    private readonly IMapper _mapper;

    public GetIssuesByAssigneeQueryHandler(IIssueRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueDto>> Handle(GetIssuesByAssigneeQuery request, CancellationToken cancellationToken)
    {
        var issues = await _repository.GetByAssigneeAsync(request.AssigneeUserId, cancellationToken);
        return issues.Select(i => _mapper.Map<IssueDto>(i)).ToList();
    }
}
