using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using BitirmeProject.NotificationService.Application.Common.Behaviors;
using BitirmeProject.NotificationService.Application.Common.Mappings;

namespace BitirmeProject.NotificationService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        var assembly = typeof(NotificationMappingProfile).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddAutoMapper(cfg => { }, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
