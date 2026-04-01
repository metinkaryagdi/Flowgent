using Xunit;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared.Abstractions.Messaging;
using Testcontainers.PostgreSql;

namespace IssueService.IntegrationTests.Fixtures;

public sealed class IssueWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("issue_test")
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
                d => d.ServiceType == typeof(DbContextOptions<IssueDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<IssueDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString())
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            // Redis → in-memory
            var redisCacheDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
            if (redisCacheDescriptor != null)
                services.Remove(redisCacheDescriptor);
            services.AddDistributedMemoryCache();

            // RabbitMQ hosted servislerini kaldır
            RemoveHostedService<Shared.Common.Messaging.OutboxPublisherService>(services);
            RemoveHostedService<BitirmeProject.IssueService.Api.Events.SprintEventsConsumer>(services);

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

            // IStorageServiceClient mock (dış Storage servisi bağımlılığı)
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageServiceClient));
            if (storageDescriptor != null)
                services.Remove(storageDescriptor);
            services.AddScoped(_ => Substitute.For<IStorageServiceClient>());
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
