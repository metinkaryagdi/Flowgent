using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Infrastructure.Clients;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using BitirmeProject.IssueService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.IssueService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIssueInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IssueDatabase");

        services.AddDbContext<IssueDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "issue:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IssueDbContext>());
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IIssueAttachmentRepository, IssueAttachmentRepository>();
        services.AddScoped<IIssueCommentRepository, IssueCommentRepository>();
        services.AddScoped<IIssueBoardRepository, IssueBoardRepository>();
        services.AddScoped<IIssueAuditRepository, IssueAuditRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IStorageServiceClient, StorageServiceClient>();

        return services;
    }
}
