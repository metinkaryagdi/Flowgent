using AutoMapper;
using System.Text.Json;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, NotificationDto>
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public MarkNotificationReadCommandHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
    }

    public async Task<NotificationDto> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            throw new NotFoundException("Notification", request.NotificationId);

        if (notification.UserId != request.UserId)
            throw new BusinessRuleException("Notification does not belong to user.");

        notification.MarkAsRead();
        await _repository.UpdateAsync(notification, cancellationToken);

        var evt = new NotificationReadEvent(
            notification.Id,
            notification.UserId,
            notification.UpdatedAt ?? DateTime.UtcNow,
            Guid.Empty);

        var outbox = new OutboxMessage
        {
            EventType = evt.GetType().Name,
            Payload = JsonSerializer.Serialize(evt),
            OccurredOn = evt.OccurredOn
        };

        await _outboxRepository.AddAsync(outbox, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<NotificationDto>(notification);
    }
}
