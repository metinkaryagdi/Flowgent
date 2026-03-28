using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AttachFile;

// FileName, ContentType, SizeBytes are fetched server-side from StorageService.
// UploadedByUserId must come from Claims in the controller, never from the request body.
// BearerToken is forwarded from the controller so the handler can authenticate against StorageService.
public sealed record AttachFileCommand(
    Guid IssueId,
    Guid FileId,
    Guid UploadedByUserId,
    string? BearerToken,
    Guid? CorrelationId) : IRequest<IssueAttachmentDto>;

// Body DTO: only the fileId is accepted from the client.
public sealed record AttachFileRequest(Guid FileId);
