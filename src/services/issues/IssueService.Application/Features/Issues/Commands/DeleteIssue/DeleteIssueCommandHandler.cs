using BitirmeProject.IssueService.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.DeleteIssue;

public sealed class DeleteIssueCommandHandler : IRequestHandler<DeleteIssueCommand>
{
    private readonly IIssueRepository _issueRepository;
    private readonly IIssueCommentRepository _commentRepository;
    private readonly IIssueAttachmentRepository _attachmentRepository;
    private readonly IIssueAuditRepository _auditRepository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;

    public DeleteIssueCommandHandler(
        IIssueRepository issueRepository,
        IIssueCommentRepository commentRepository,
        IIssueAttachmentRepository attachmentRepository,
        IIssueAuditRepository auditRepository,
        IIssueBoardRepository boardRepository,
        IUnitOfWork unitOfWork,
        IDistributedCache cache)
    {
        _issueRepository = issueRepository;
        _commentRepository = commentRepository;
        _attachmentRepository = attachmentRepository;
        _auditRepository = auditRepository;
        _boardRepository = boardRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(DeleteIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _issueRepository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new NotFoundException("Issue", request.IssueId);

        await _commentRepository.RemoveByIssueIdAsync(issue.Id, cancellationToken);
        await _attachmentRepository.RemoveByIssueIdAsync(issue.Id, cancellationToken);
        await _auditRepository.RemoveByIssueIdAsync(issue.Id, cancellationToken);
        await _boardRepository.RemoveByIssueIdAsync(issue.Id, cancellationToken);
        await _issueRepository.RemoveAsync(issue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try { await _cache.RemoveAsync($"board:project:{issue.ProjectId}:{issue.OrganizationId}", cancellationToken); } catch { }
    }
}
