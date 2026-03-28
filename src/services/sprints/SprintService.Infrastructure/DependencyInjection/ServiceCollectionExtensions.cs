using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Infrastructure.Clients;
using BitirmeProject.SprintService.Infrastructure.Persistence;
using BitirmeProject.SprintService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.SprintService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSprintInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SprintDatabase");

        services.AddDbContext<SprintDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SprintDbContext>());
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ISprintIssueRepository, SprintIssueRepository>();
        services.AddScoped<ISprintSummaryRepository, SprintSummaryRepository>();
        services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIssueServiceClient, IssueServiceClient>();

        var issueServiceUrl = configuration["IssueService:BaseUrl"] ?? "http://issue-service:8080/";
        services.AddHttpClient("IssueService", client =>
        {
            client.BaseAddress = new Uri(issueServiceUrl);
        });

        return services;
    }
}
