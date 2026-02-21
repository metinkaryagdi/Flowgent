using BitirmeProject.NotificationService.Application.Abstractions;
using BitirmeProject.NotificationService.Domain.Entities;
using BitirmeProject.NotificationService.Domain.Enums;
using BitirmeProject.NotificationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.NotificationService.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _dbContext;

    public NotificationRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<Notification?> GetByExternalEventIdAsync(Guid externalEventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications.FirstOrDefaultAsync(n => n.ExternalEventId == externalEventId, cancellationToken);
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Update(notification);
        return Task.CompletedTask;
    }
}
