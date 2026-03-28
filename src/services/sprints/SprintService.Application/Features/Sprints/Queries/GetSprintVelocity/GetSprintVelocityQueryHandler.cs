using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintVelocity;

public sealed class GetSprintVelocityQueryHandler : IRequestHandler<GetSprintVelocityQuery, SprintVelocityDto>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintIssueRepository _issueRepository;
    private readonly ISprintSummaryRepository _summaryRepository;

    public GetSprintVelocityQueryHandler(
        ISprintRepository sprintRepository,
        ISprintIssueRepository issueRepository,
        ISprintSummaryRepository summaryRepository)
    {
        _sprintRepository = sprintRepository;
        _issueRepository = issueRepository;
        _summaryRepository = summaryRepository;
    }

    public async Task<SprintVelocityDto> Handle(GetSprintVelocityQuery request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            throw new NotFoundException("Sprint", request.SprintId);

        // Completed sprint: return the immutable snapshot
        if (sprint.Status == SprintStatus.Completed)
        {
            var snapshot = await _summaryRepository.GetBySprintIdAsync(request.SprintId, cancellationToken);
            if (snapshot is not null)
            {
                return new SprintVelocityDto
                {
                    SprintId = snapshot.SprintId,
                    TotalIssues = snapshot.TotalIssues,
                    DoneIssues = snapshot.CompletedIssues,
                    IsSnapshot = true,
                    SnapshotTakenAt = snapshot.SnapshotTakenAt
                };
            }
            // Snapshot missing (sprint completed before Faz 13) — fall through to live calculation
        }

        // Active / Planned sprint: calculate live
        var issues = await _issueRepository.GetBySprintIdAsync(request.SprintId, cancellationToken);
        return new SprintVelocityDto
        {
            SprintId = request.SprintId,
            TotalIssues = issues.Count,
            DoneIssues = issues.Count(i => string.Equals(i.Status, "Done", StringComparison.OrdinalIgnoreCase)),
            IsSnapshot = false,
            SnapshotTakenAt = null
        };
    }
}
