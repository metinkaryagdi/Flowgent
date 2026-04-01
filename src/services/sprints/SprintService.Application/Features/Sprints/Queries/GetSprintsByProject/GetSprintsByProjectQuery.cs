using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintsByProject;

public sealed record GetSprintsByProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<SprintDto>>;
