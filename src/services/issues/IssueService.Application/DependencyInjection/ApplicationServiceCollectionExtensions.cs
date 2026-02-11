using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using BitirmeProject.IssueService.Application.Common.Behaviors;
using BitirmeProject.IssueService.Application.Common.Mappings;

namespace BitirmeProject.IssueService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIssueApplication(this IServiceCollection services)
    {
        var assembly = typeof(IssueMappingProfile).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddAutoMapper(cfg => { }, assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}