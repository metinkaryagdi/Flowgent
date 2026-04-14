using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Sprints.Commands.SuggestBalance;

public sealed record SuggestBalanceCommand(
    Guid SprintId,
    Guid ProjectId,
    Guid UserId,
    Guid OrganizationId
) : IRequest<SuggestBalanceResultDto>;
