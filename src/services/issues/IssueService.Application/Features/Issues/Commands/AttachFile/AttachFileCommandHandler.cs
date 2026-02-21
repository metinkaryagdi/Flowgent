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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AttachFileCommandHandler(
        IIssueRepository issueRepository,
        IIssueAttachmentRepository attachmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _issueRepository = issueRepository;
        _attachmentRepository = attachmentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IssueAttachmentDto> Handle(AttachFileCommand request, CancellationToken cancellationToken)
    {
        var issue = await _issueRepository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new NotFoundException("Issue", request.IssueId);

        var attachment = new IssueAttachment(
            request.IssueId,
            request.FileId,
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            request.UploadedByUserId);

        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<IssueAttachmentDto>(attachment);
    }
}
