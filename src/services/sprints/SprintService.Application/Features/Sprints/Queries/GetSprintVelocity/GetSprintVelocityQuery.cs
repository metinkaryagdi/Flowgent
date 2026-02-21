using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintVelocity;

public sealed record GetSprintVelocityQuery(Guid SprintId) : IRequest<SprintVelocityDto>;
