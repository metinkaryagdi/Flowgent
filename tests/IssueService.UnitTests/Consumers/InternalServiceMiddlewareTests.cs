using BitirmeProject.IssueService.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IssueService.UnitTests.Consumers;

/// <summary>
/// Security regression — Scenario 6: Internal header spoof.
/// Verifies that InternalServiceMiddleware rejects requests with invalid or spoofed
/// X-Internal-Service-Key / caller headers.
/// </summary>
public sealed class InternalServiceMiddlewareTests
{
    private const string ValidKey = "test-internal-key-1234";
    private const string ValidCaller = "SprintService";

    private static InternalServiceMiddleware BuildMiddleware(RequestDelegate next)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalService:ApiKey"] = ValidKey
            })
            .Build();

        return new InternalServiceMiddleware(next, config, NullLogger<InternalServiceMiddleware>.Instance);
    }

    private static DefaultHttpContext BuildContext(string? callerName, string? apiKey, string? userId)
    {
        var context = new DefaultHttpContext();
        if (callerName is not null)
            context.Request.Headers["X-Internal-Service"] = callerName;
        if (apiKey is not null)
            context.Request.Headers["X-Internal-Service-Key"] = apiKey;
        if (userId is not null)
            context.Request.Headers["X-User-Id"] = userId;
        return context;
    }

    [Fact]
    public async Task SpoofedCaller_WithWrongApiKey_Returns403()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = BuildMiddleware(next);

        var context = BuildContext(ValidCaller, "wrong-key", Guid.NewGuid().ToString());
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse("middleware must short-circuit before calling next");
    }

    [Fact]
    public async Task SpoofedCaller_UnknownCallerName_Returns403()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = BuildMiddleware(next);

        var context = BuildContext("EvilService", ValidKey, Guid.NewGuid().ToString());
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SpoofedCaller_ValidKeyAndCallerButInvalidUserId_Returns403()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = BuildMiddleware(next);

        var context = BuildContext(ValidCaller, ValidKey, "not-a-guid");
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidInternalCall_PassesThroughAndSetsUserClaims()
    {
        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };
        var middleware = BuildMiddleware(next);

        var userId = Guid.NewGuid().ToString();
        var context = BuildContext(ValidCaller, ValidKey, userId);
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("valid internal calls must proceed");
        context.User.Identity?.IsAuthenticated.Should().BeTrue();
        context.User.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == userId);
        context.User.Claims.Should().Contain(c => c.Type == "internal_call" && c.Value == "true");
    }

    [Fact]
    public async Task RequestWithNoInternalHeader_PassesThrough_AsUnauthenticated()
    {
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = BuildMiddleware(next);

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue("normal (non-internal) requests must not be blocked");
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
