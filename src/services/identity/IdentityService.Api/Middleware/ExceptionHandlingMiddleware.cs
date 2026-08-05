using System.Net;
using System.Text.Json;
using BitirmeProject.IdentityService.Application.Common;
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

        // A lockout is an authentication failure, so it must answer 401 like every other
        // rejected login -- not 400. The code lets clients distinguish it from a plain
        // bad password without parsing the message.
        if (exception is AccountLockedException locked)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = locked.Message,
                code = AccountLockedException.Code,
                lockoutEnd = locked.LockoutEnd
            }));
            return;
        }

        // Same reasoning as the lockout above: refusing an unverified account is an
        // authentication failure (401), and the code lets the client offer "resend the
        // link" instead of the misleading "wrong password".
        if (exception is EmailNotVerifiedException unverified)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = unverified.Message,
                code = EmailNotVerifiedException.Code,
                email = unverified.Email
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
