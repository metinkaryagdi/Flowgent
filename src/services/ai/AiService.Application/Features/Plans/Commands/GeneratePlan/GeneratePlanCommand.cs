using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Plans.Commands.GeneratePlan;

public sealed record GeneratePlanCommand(
    Guid ProjectId,
    Guid UserId,
    Guid OrganizationId,
    string Description
) : IRequest<GeneratePlanResultDto>;
