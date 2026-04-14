using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Sprints.Queries.AssessRisk;

public sealed record AssessSprintRiskQuery(
    Guid SprintId,
    Guid ProjectId,
    Guid UserId,
    Guid OrganizationId
) : IRequest<SprintRiskResultDto>;
