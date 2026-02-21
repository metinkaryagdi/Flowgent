using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.StartSprint;

public sealed record StartSprintCommand(
    Guid SprintId,
    Guid StartedByUserId,
    Guid? CorrelationId) : IRequest<SprintDto>;
