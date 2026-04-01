using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shared.Common.Messaging;

namespace BitirmeProject.NotificationService.Api.Health;

public sealed class NotificationDlqHealthCheck : IHealthCheck
{
    private static readonly string[] DlqNames =
    {
        RabbitMqTopology.GetDeadLetterQueueName("NotificationService", "IssueAssignedEvent"),
        RabbitMqTopology.GetDeadLetterQueueName("NotificationService", "IssueStatusChangedEvent"),
        RabbitMqTopology.GetDeadLetterQueueName("NotificationService", "CommentAddedEvent"),
        RabbitMqTopology.GetDeadLetterQueueName("NotificationService", "MemberAddedEvent")
    };

    private readonly IConfiguration _configuration;

    public NotificationDlqHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["RabbitMQ:Host"] ?? "rabbitmq";
        var port = int.TryParse(_configuration["RabbitMQ:Port"], out var parsedPort) ? parsedPort : 5672;
        var user = _configuration["RabbitMQ:Username"] ?? "admin";
        var pass = _configuration["RabbitMQ:Password"] ?? "admin123";
        var vhost = _configuration["RabbitMQ:VirtualHost"] ?? "/";

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = pass,
                VirtualHost = vhost
            };

            using var connection = factory.CreateConnection();

            var queueDepths = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            uint totalDepth = 0;

            foreach (var queueName in DlqNames)
            {
                // Use a fresh channel per queue: OperationInterruptedException closes the channel,
                // so reusing a closed channel would corrupt subsequent iterations.
                using var queueChannel = connection.CreateModel();
                try
                {
                    var result = queueChannel.QueueDeclarePassive(queueName);
                    queueDepths[queueName] = result.MessageCount;
                    totalDepth += result.MessageCount;
                }
                catch (OperationInterruptedException)
                {
                    queueDepths[queueName] = 0u;
                }
            }

            queueDepths["totalDepth"] = totalDepth;

            if (totalDepth > 0)
                return Task.FromResult(HealthCheckResult.Degraded("Notification DLQ contains messages.", data: queueDepths));

            return Task.FromResult(HealthCheckResult.Healthy("Notification DLQ is empty.", queueDepths));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Notification DLQ health check failed.", ex));
        }
    }
}
