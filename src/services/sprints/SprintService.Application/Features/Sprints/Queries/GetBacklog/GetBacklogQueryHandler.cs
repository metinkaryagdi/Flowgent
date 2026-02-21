using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetBacklog;

public sealed class GetBacklogQueryHandler : IRequestHandler<GetBacklogQuery, IReadOnlyList<SprintIssueDto>>
{
    private readonly ISprintIssueRepository _issueRepository;
    private readonly IMapper _mapper;

    public GetBacklogQueryHandler(ISprintIssueRepository issueRepository, IMapper mapper)
    {
        _issueRepository = issueRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SprintIssueDto>> Handle(GetBacklogQuery request, CancellationToken cancellationToken)
    {
        var items = await _issueRepository.GetBacklogByProjectIdAsync(request.ProjectId, cancellationToken);
        return items.Select(item => _mapper.Map<SprintIssueDto>(item)).ToList();
    }
}
