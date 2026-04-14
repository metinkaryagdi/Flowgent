using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Sprints.Commands.GenerateRetrospective;

public sealed record GenerateRetrospectiveCommand(
    Guid SprintId,
    Guid ProjectId,
    Guid UserId,
    Guid OrganizationId
) : IRequest<RetrospectiveResultDto>;
