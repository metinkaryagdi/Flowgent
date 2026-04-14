using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Enums;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.UpdateIssue;

public sealed record UpdateIssueCommand(
    Guid IssueId,
    string? Title,
    string? Description,
    IssuePriority? Priority,
    int ExpectedVersion) : IRequest<IssueDto>;
