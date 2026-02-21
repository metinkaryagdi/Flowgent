using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AttachFile;

public sealed record AttachFileCommand(
    Guid IssueId,
    Guid FileId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId,
    Guid? CorrelationId) : IRequest<IssueAttachmentDto>;
