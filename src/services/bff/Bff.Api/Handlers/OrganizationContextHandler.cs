using Microsoft.AspNetCore.Http;

namespace BitirmeProject.Bff.Api.Handlers;

/// <summary>
/// Forwards org_id and org_role claims from the authenticated user to downstream service calls.
/// </summary>
public sealed class OrganizationContextHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrganizationContextHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is not null)
        {
            var orgId = user.FindFirst("org_id")?.Value;
            var orgRole = user.FindFirst("org_role")?.Value;

            if (!string.IsNullOrEmpty(orgId))
                request.Headers.TryAddWithoutValidation("X-Organization-Id", orgId);

            if (!string.IsNullOrEmpty(orgRole))
                request.Headers.TryAddWithoutValidation("X-Organization-Role", orgRole);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
