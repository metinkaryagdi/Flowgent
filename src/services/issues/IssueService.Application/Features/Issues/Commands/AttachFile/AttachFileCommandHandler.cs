using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Entities;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AttachFile;

public sealed class AttachFileCommandHandler : IRequestHandler<AttachFileCommand, IssueAttachmentDto>
{
    private readonly IIssueRepository _issueRepository;
    private readonly IIssueAttachmentRepository _attachmentRepository;
    private readonly IStorageServiceClient _storageClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AttachFileCommandHandler(
        IIssueRepository issueRepository,
        IIssueAttachmentRepository attachmentRepository,
        IStorageServiceClient storageClient,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _issueRepository = issueRepository;
        _attachmentRepository = attachmentRepository;
        _storageClient = storageClient;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IssueAttachmentDto> Handle(AttachFileCommand request, CancellationToken cancellationToken)
    {
        var issue = await _issueRepository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new NotFoundException("Issue", request.IssueId);

        // Duplicate attachment guard (DB unique index is the hard guarantee; this gives a friendly error first)
        var alreadyAttached = await _attachmentRepository.ExistsAsync(request.IssueId, request.FileId, cancellationToken);
        if (alreadyAttached)
            throw new BusinessRuleException($"File {request.FileId} is already attached to this issue.");

        // BearerToken is forwarded from the controller so StorageService can authenticate the request
        var fileMetadata = await _storageClient.GetFileMetadataAsync(request.FileId, request.BearerToken, cancellationToken);
        if (fileMetadata is null)
            throw new NotFoundException("File", request.FileId);

        // Require the file to be finalized before it can be linked (status 1 = Finalized)
        if (fileMetadata.Status != 1)
            throw new BusinessRuleException("File must be finalized in StorageService before it can be attached to an issue.");

        var attachment = new IssueAttachment(
            request.IssueId,
            request.FileId,
            fileMetadata.FileName,
            fileMetadata.ContentType,
            fileMetadata.SizeBytes,
            request.UploadedByUserId);

        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<IssueAttachmentDto>(attachment);
    }
}
