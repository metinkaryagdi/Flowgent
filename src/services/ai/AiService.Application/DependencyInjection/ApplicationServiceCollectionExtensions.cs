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

        services.AddScoped<ITool, CreateIssueTool>();
        services.AddScoped<ITool, CreateSprintTool>();
        services.AddScoped<ITool, AddIssueToSprintTool>();
        services.AddScoped<ITool, GetActiveSprintTool>();
        services.AddScoped<ITool, GetProjectIssuesTool>();
        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<AgentLoop>();

        return services;
    }
}
