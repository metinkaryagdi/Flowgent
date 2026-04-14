using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Issues.Queries.DetectDuplicate;

public sealed record DetectDuplicateQuery(
    Guid ProjectId,
    Guid UserId,
    Guid OrganizationId,
    string Title
) : IRequest<DetectDuplicateResultDto>;
