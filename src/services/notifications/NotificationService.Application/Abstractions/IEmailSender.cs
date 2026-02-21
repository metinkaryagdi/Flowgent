namespace BitirmeProject.NotificationService.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(Guid userId, string subject, string body, CancellationToken cancellationToken = default);
}
