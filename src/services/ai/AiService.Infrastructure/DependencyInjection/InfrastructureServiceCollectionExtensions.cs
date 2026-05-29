using BitirmeProject.AiService.Application.Abstractions;
using BitirmeProject.AiService.Infrastructure.Clients;
using BitirmeProject.AiService.Infrastructure.Options;
using BitirmeProject.AiService.Infrastructure.Persistence;
using BitirmeProject.AiService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BitirmeProject.AiService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AiDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AiDatabase")));

        // Repository
        services.AddScoped<IAiSessionRepository, AiSessionRepository>();
        services.AddScoped<IAiToolExecutionRepository, AiToolExecutionRepository>();
        services.Configure<InternalServiceOptions>(configuration.GetSection("InternalService"));

        // Model selector — runtime'da fine-tune ↔ base toggle edilebilsin diye singleton.
        services.AddSingleton<IModelSelector, ModelSelector>();

        // Ollama HTTP client
        services.AddHttpClient<IOllamaClient, OllamaClient>(client =>
        {
            var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://ollama:11434";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(3); // LLM calls can be slow
        });

        // Internal service clients
        services.AddHttpClient<ISprintServiceClient, SprintServiceClient>(client =>
        {
            var baseUrl = configuration["Services:SprintService"] ?? "http://sprint-api:8080";
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddHttpClient<IIssueServiceClient, IssueServiceClient>(client =>
        {
            var baseUrl = configuration["Services:IssueService"] ?? "http://issue-api:8080";
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}
