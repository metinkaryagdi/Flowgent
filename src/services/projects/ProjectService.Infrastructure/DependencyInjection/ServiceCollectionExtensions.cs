using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using BitirmeProject.ProjectService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;

namespace BitirmeProject.ProjectService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProjectDatabase");

        services.AddDbContext<ProjectDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProjectDbContext>());
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }
}