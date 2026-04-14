using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;

public sealed record CreateIssueCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    IssuePriority Priority,
    Guid CreatedByUserId,
    Guid? CorrelationId,
    Guid? OrganizationId = null) : IRequest<IssueDto>;