using Xunit;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared.Abstractions.Messaging;
using Testcontainers.PostgreSql;

namespace IdentityService.IntegrationTests.Fixtures;

public sealed class IdentityWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("identity_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>Captures the verification links the app "sends", so tests can follow them.</summary>
    public CapturingEmailService Emails { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // AdminUserSeeder refuses to run without an explicit password, so the test
        // host has to supply one the same way a real deployment would.
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SEED_ADMIN"] = "true",
                ["ADMIN_PASSWORD"] = "IntegrationTest!Admin1",
                ["ADMIN_EMAIL"] = "admin@bitirme.local",
                ["ADMIN_USERNAME"] = "admin"
            });
        });

        builder.ConfigureServices(services =>
        {
            // PostgreSQL: üretim bağlantısını kaldır, test container'ını kullan
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<IdentityDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString())
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            // SMTP: no relay exists in tests, and the issuer swallows send failures, so
            // without this the verification token would be generated and then lost.
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
                services.Remove(emailDescriptor);
            services.AddSingleton<IEmailService>(Emails);

            // RabbitMQ hosted servislerini kaldır
            RemoveHostedService<Shared.Common.Messaging.OutboxPublisherService>(services);

            // IEventBus'ı mock ile değiştir
            var eventBusDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
            if (eventBusDescriptor != null)
                services.Remove(eventBusDescriptor);
            services.AddSingleton(Substitute.For<IEventBus>());

            // OutboxPublisherMonitor kaldır
            var monitorDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Shared.Common.Health.OutboxPublisherMonitor));
            if (monitorDescriptor != null)
                services.Remove(monitorDescriptor);
            services.AddSingleton(new Shared.Common.Health.OutboxPublisherMonitor());
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
