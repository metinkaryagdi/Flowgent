using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using BitirmeProject.SprintService.Application.Common.Behaviors;
using BitirmeProject.SprintService.Application.Common.Mappings;

namespace BitirmeProject.SprintService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSprintApplication(this IServiceCollection services)
    {
        var assembly = typeof(SprintMappingProfile).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddAutoMapper(cfg => { }, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
