using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;

public sealed record ChangeIssueStatusCommand(
    Guid IssueId,
    IssueStatus NewStatus,
    Guid ChangedByUserId,
    Guid? CorrelationId) : IRequest<IssueDto>;