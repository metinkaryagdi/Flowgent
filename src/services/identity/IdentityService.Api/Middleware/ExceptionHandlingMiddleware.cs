using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeProject.IdentityService.Api.Middleware;

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
        context.Response.ContentType = "application/json";

        if (exception is ValidationException ve)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var fieldErrors = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Validation failed.",
                errors = fieldErrors
            }));
            return;
        }

        var (statusCode, message) = exception switch
        {
            InvalidOperationException ioe when ioe.Message.Contains("credentials", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.Unauthorized, ioe.Message),
            InvalidOperationException ioe => (HttpStatusCode.BadRequest, ioe.Message),
            UnauthorizedAccessException uae => (HttpStatusCode.Unauthorized, uae.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
    }
}
