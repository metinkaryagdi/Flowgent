using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesBySprint;

public sealed class GetIssuesBySprintQueryHandler : IRequestHandler<GetIssuesBySprintQuery, IReadOnlyList<IssueDto>>
{
    private readonly IIssueRepository _repository;
    private readonly IMapper _mapper;

    public GetIssuesBySprintQueryHandler(IIssueRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueDto>> Handle(GetIssuesBySprintQuery request, CancellationToken cancellationToken)
    {
        var issues = await _repository.GetBySprintIdAsync(request.SprintId, cancellationToken);
        return issues.Select(issue => _mapper.Map<IssueDto>(issue)).ToList();
    }
}
