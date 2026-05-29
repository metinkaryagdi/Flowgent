using AutoMapper;
using System.Text.Json;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MarkNotificationReadCommandHandler> _logger;

    public MarkNotificationReadCommandHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository,
        IMapper mapper,
        ILogger<MarkNotificationReadCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<NotificationDto> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "MarkAsRead requested. NotificationId={NotificationId} CallerUserId={CallerUserId}",
            request.NotificationId, request.UserId);

        var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
        {
            _logger.LogWarning("MarkAsRead: notification not found. NotificationId={NotificationId}", request.NotificationId);
            throw new NotFoundException("Notification", request.NotificationId);
        }

        if (notification.UserId != request.UserId)
        {
            _logger.LogWarning(
                "MarkAsRead: user mismatch. NotificationOwner={Owner} Caller={Caller}",
                notification.UserId, request.UserId);
            throw new BusinessRuleException("Notification does not belong to user.");
        }

        var beforeIsRead = notification.IsRead;
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
        var rows = await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "MarkAsRead saved. NotificationId={NotificationId} BeforeIsRead={Before} AfterIsRead={After} RowsAffected={Rows}",
            request.NotificationId, beforeIsRead, notification.IsRead, rows);

        return _mapper.Map<NotificationDto>(notification);
    }
}
