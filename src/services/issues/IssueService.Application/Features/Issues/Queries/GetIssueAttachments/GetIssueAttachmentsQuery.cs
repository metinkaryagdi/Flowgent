using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueAttachments;

public sealed record GetIssueAttachmentsQuery(Guid IssueId) : IRequest<IReadOnlyList<IssueAttachmentDto>>;
