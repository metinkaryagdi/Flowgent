using System.Text.Json;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Entities;
using MediatR;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;

public sealed class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, IssueDto>
{
    private readonly IIssueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public CreateIssueCommandHandler(IIssueRepository repository, IUnitOfWork unitOfWork, IOutboxRepository outboxRepository, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<IssueDto> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = new Issue(request.ProjectId, request.Title, request.Description, request.Priority, request.CreatedByUserId);
        await _repository.AddAsync(issue, cancellationToken);

        var evt = new IssueCreatedEvent(issue.Id, issue.ProjectId, issue.Title, "Task", issue.Priority.ToString(), issue.CreatedByUserId, request.CorrelationId ?? Guid.Empty);
        var outbox = new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        };
        await _outboxRepository.AddAsync(outbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<IssueDto>(issue);
    }
}