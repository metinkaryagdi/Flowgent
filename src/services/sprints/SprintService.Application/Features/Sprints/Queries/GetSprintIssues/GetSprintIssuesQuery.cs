using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintIssues;

public sealed record GetSprintIssuesQuery(Guid SprintId) : IRequest<IReadOnlyList<SprintIssueDto>>;
