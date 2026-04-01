using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.IssueService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, extensions) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Not Found", exception.Message, null),
            ValidationException ve => (HttpStatusCode.BadRequest, "Validation Error", exception.Message, new Dictionary<string, object?>
            {
                ["errors"] = ve.Errors
            }),
            BusinessRuleException => (HttpStatusCode.BadRequest, "Business Rule Violation", exception.Message, null),
            ConcurrencyException => (HttpStatusCode.Conflict, "Concurrency Conflict", exception.Message, null),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Concurrency Conflict", "The issue was updated by another request.", null),
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
