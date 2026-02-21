using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintIssues;

public sealed class GetSprintIssuesQueryHandler : IRequestHandler<GetSprintIssuesQuery, IReadOnlyList<SprintIssueDto>>
{
    private readonly ISprintIssueRepository _issueRepository;
    private readonly IMapper _mapper;

    public GetSprintIssuesQueryHandler(ISprintIssueRepository issueRepository, IMapper mapper)
    {
        _issueRepository = issueRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SprintIssueDto>> Handle(GetSprintIssuesQuery request, CancellationToken cancellationToken)
    {
        var items = await _issueRepository.GetBySprintIdAsync(request.SprintId, cancellationToken);
        return items.Select(item => _mapper.Map<SprintIssueDto>(item)).ToList();
    }
}
