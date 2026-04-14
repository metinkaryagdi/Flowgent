using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.DeleteIssue;

public sealed record DeleteIssueCommand(Guid IssueId) : IRequest;
