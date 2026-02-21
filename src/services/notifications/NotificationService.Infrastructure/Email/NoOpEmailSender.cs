using BitirmeProject.NotificationService.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace BitirmeProject.NotificationService.Infrastructure.Email;

public sealed class NoOpEmailSender : IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;

    public NoOpEmailSender(ILogger<NoOpEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Guid userId, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email notification (no-op). UserId={UserId}, Subject={Subject}",
            userId,
            subject);

        return Task.CompletedTask;
    }
}
