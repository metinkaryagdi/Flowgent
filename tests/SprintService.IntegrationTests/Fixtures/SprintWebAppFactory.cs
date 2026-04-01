using Xunit;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared.Abstractions.Messaging;
using Testcontainers.PostgreSql;

namespace SprintService.IntegrationTests.Fixtures;

public sealed class SprintWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("sprint_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // PostgreSQL
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SprintDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<SprintDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString())
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            // RabbitMQ hosted servislerini kaldır
            RemoveHostedService<Shared.Common.Messaging.OutboxPublisherService>(services);
            RemoveHostedService<BitirmeProject.SprintService.Api.Events.IssueEventsConsumer>(services);

            // IEventBus mock
            var eventBusDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
            if (eventBusDescriptor != null)
                services.Remove(eventBusDescriptor);
            services.AddSingleton(Substitute.For<IEventBus>());

            // OutboxPublisherMonitor
            var monitorDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Shared.Common.Health.OutboxPublisherMonitor));
            if (monitorDescriptor != null)
                services.Remove(monitorDescriptor);
            services.AddSingleton(new Shared.Common.Health.OutboxPublisherMonitor());

            // IIssueServiceClient mock (dış Issue servisi bağımlılığı)
            var issueClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IIssueServiceClient));
            if (issueClientDescriptor != null)
                services.Remove(issueClientDescriptor);

            var issueClientMock = Substitute.For<IIssueServiceClient>();
            // Varsayılan davranış: null döndür (issue bulunamadı senaryosu)
            issueClientMock
                .GetIssueAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns((IssueMetadataDto?)null);
            services.AddScoped(_ => issueClientMock);
        });

        builder.UseEnvironment("Testing");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
    }

    private static void RemoveHostedService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
              && d.ImplementationType == typeof(T));
        if (descriptor != null)
            services.Remove(descriptor);
    }
}
