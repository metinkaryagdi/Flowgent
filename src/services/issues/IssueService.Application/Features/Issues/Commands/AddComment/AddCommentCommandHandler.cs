using System.Text.Json;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Entities;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AddComment;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, IssueCommentDto>
{
    private readonly IIssueRepository _issueRepository;
    private readonly IIssueCommentRepository _commentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public AddCommentCommandHandler(
        IIssueRepository issueRepository,
        IIssueCommentRepository commentRepository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _issueRepository = issueRepository;
        _commentRepository = commentRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<IssueCommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var issue = await _issueRepository.GetByIdAsync(request.IssueId, cancellationToken);
        if (issue is null)
            throw new NotFoundException("Issue", request.IssueId);

        var comment = new IssueComment(request.IssueId, request.AuthorUserId, request.Content);
        await _commentRepository.AddAsync(comment, cancellationToken);

        var evt = new CommentAddedEvent(comment.Id, issue.Id, issue.ProjectId, request.AuthorUserId, issue.Title, issue.CreatedByUserId, issue.AssigneeUserId, request.CorrelationId ?? Guid.Empty);
        var outbox = new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        };
        await _outboxRepository.AddAsync(outbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<IssueCommentDto>(comment);
    }
}
