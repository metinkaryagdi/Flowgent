using System.Reflection;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using BitirmeProject.ProjectService.Application.Common.Behaviors;
using BitirmeProject.ProjectService.Application.Common.Mappings;

namespace BitirmeProject.ProjectService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddProjectApplication(this IServiceCollection services)
    {
        var assembly = typeof(ProjectMappingProfile).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddAutoMapper(cfg => { }, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}