using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetBacklog;

public sealed record GetBacklogQuery(Guid ProjectId) : IRequest<IReadOnlyList<SprintIssueDto>>;
