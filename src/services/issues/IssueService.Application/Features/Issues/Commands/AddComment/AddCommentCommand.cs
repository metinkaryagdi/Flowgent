using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AddComment;

public sealed record AddCommentCommand(
    Guid IssueId,
    Guid AuthorUserId,
    string Content,
    Guid? CorrelationId) : IRequest<IssueCommentDto>;
