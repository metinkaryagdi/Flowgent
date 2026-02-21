using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetActiveSprint;

public sealed record GetActiveSprintQuery(Guid ProjectId) : IRequest<SprintDto?>;
