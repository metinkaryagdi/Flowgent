using AutoMapper;
using System.Text.Json;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Domain.Enums;
using MediatR;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;
using Shared.Contracts.Events;

namespace BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public sealed class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMapper _mapper;

    public CreateNotificationCommandHandler(
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

    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var channel = ParseChannel(request.Channel);

        if (request.ExternalEventId.HasValue)
        {
            var existing = await _repository.GetByExternalEventIdAsync(request.ExternalEventId.Value, cancellationToken);
            if (existing is not null)
                return _mapper.Map<NotificationDto>(existing);
        }

        var notification = new Notification(
            request.UserId,
            request.Title,
            request.Message,
            channel,
            request.EntityType,
            request.EntityId,
            request.ExternalEventId);

        await _repository.AddAsync(notification, cancellationToken);

        var evt = new NotificationCreatedEvent(
            notification.Id,
            notification.UserId,
            notification.Title,
            notification.Message,
            notification.Channel.ToString(),
            notification.Status.ToString(),
            notification.EntityType,
            notification.EntityId,
            notification.ExternalEventId,
            request.CorrelationId ?? Guid.Empty);

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

    private static NotificationChannel ParseChannel(string channel)
    {
        if (string.Equals(channel, "inapp", StringComparison.OrdinalIgnoreCase))
            return NotificationChannel.InApp;

        if (string.Equals(channel, "email", StringComparison.OrdinalIgnoreCase))
            return NotificationChannel.Email;

        throw new BusinessRuleException($"Unsupported notification channel: {channel}");
    }
}
