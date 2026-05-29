using System.Reflection;
using BitirmeProject.AiService.Application.Tools;
using BitirmeProject.AiService.Application.Tools.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace BitirmeProject.AiService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAiApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Sıra tools/ai_data_collector/prompts/agent.py TOOL_CATALOG ile birebir aynı olmalı —
        // training data'sının system prompt'undaki catalog JSON'u ile inference'ın ToolRegistry
        // catalog'u byte-eşleşecek. Yeni tool eklenirse her iki tarafa aynı pozisyona ekle.
        services.AddScoped<ITool, GetActiveSprintTool>();
        services.AddScoped<ITool, GetProjectIssuesTool>();
        services.AddScoped<ITool, CreateIssueTool>();
        services.AddScoped<ITool, CreateSprintTool>();
        services.AddScoped<ITool, AddIssueToSprintTool>();
        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<AgentLoop>();

        return services;
    }
}
