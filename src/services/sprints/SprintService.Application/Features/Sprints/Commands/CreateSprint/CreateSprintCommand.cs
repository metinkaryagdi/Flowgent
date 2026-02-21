using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;

public sealed record CreateSprintCommand(
    Guid ProjectId,
    string Name,
    string? Goal,
    Guid CreatedByUserId,
    Guid? CorrelationId) : IRequest<SprintDto>;
