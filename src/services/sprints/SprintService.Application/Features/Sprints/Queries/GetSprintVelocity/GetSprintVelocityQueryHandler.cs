using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintVelocity;

public sealed class GetSprintVelocityQueryHandler : IRequestHandler<GetSprintVelocityQuery, SprintVelocityDto>
{
    private readonly ISprintIssueRepository _issueRepository;

    public GetSprintVelocityQueryHandler(ISprintIssueRepository issueRepository)
    {
        _issueRepository = issueRepository;
    }

    public async Task<SprintVelocityDto> Handle(GetSprintVelocityQuery request, CancellationToken cancellationToken)
    {
        var issues = await _issueRepository.GetBySprintIdAsync(request.SprintId, cancellationToken);
        var total = issues.Count;
        var done = issues.Count(i => string.Equals(i.Status, "Done", StringComparison.OrdinalIgnoreCase));

        return new SprintVelocityDto
        {
            SprintId = request.SprintId,
            TotalIssues = total,
            DoneIssues = done
        };
    }
}
