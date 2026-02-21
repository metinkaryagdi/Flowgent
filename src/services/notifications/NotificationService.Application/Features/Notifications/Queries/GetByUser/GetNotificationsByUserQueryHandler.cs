using AutoMapper;
using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Application.DTOs;
using MediatR;

namespace BitirmeProject.NotificationService.Application.Features.Notifications.Queries.GetByUser;

public sealed class GetNotificationsByUserQueryHandler : IRequestHandler<GetNotificationsByUserQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _repository;
    private readonly IMapper _mapper;

    public GetNotificationsByUserQueryHandler(INotificationRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsByUserQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return notifications.Select(n => _mapper.Map<NotificationDto>(n)).ToList();
    }
}
