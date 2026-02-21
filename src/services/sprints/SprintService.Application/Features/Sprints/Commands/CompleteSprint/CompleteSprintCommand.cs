using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.CompleteSprint;

public sealed record CompleteSprintCommand(
    Guid SprintId,
    Guid CompletedByUserId,
    Guid? CorrelationId) : IRequest<SprintDto>;
