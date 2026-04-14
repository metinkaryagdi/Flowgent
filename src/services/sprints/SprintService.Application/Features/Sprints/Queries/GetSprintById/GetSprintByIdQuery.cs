using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintById;

public sealed record GetSprintByIdQuery(Guid SprintId) : IRequest<SprintDto?>;
