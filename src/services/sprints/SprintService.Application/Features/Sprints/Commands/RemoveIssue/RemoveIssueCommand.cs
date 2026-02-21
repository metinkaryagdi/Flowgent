using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.RemoveIssue;

public sealed record RemoveIssueCommand(
    Guid SprintId,
    Guid IssueId,
    Guid RemovedByUserId,
    Guid? CorrelationId) : IRequest<SprintIssueDto>;
