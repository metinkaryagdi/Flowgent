using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Issues.Commands.EnrichIssue;

public sealed record EnrichIssueCommand(
    Guid IssueId,
    string Title,
    Guid ProjectId,
    Guid UserId,
    Guid OrganizationId
) : IRequest<EnrichIssueResultDto>;
