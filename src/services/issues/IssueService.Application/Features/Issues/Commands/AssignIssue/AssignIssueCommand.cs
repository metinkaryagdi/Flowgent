using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;

public sealed record AssignIssueCommand(
    Guid IssueId,
    Guid AssigneeUserId,
    Guid AssignedByUserId,
    int ExpectedVersion,
    Guid? CorrelationId) : IRequest<IssueDto>;
