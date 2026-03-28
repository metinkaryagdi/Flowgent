using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.AddIssue;

public sealed record AddIssueCommand(
    Guid SprintId,
    Guid IssueId,
    Guid AddedByUserId,
    Guid? CorrelationId,
    string? BearerToken = null) : IRequest<SprintIssueDto>;
