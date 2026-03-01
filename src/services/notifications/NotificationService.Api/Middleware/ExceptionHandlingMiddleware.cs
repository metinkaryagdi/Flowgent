using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.NotificationService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, extensions) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Not Found", exception.Message, null),
            BusinessRuleException => (HttpStatusCode.BadRequest, "Business Rule Violation", exception.Message, null),
            FluentValidation.ValidationException ve => (HttpStatusCode.BadRequest, "Validation Error", "One or more validation errors occurred.", new Dictionary<string, object?>
            {
                ["errors"] = ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            }),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Concurrency Conflict", "The notification was updated by another request.", null),
            _ => (HttpStatusCode.InternalServerError, "Server Error", "An unexpected error occurred.", null)
        };

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
