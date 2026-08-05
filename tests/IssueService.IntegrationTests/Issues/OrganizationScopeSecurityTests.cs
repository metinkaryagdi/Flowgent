using Xunit;
using System.Net;
using System.Net.Http.Json;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using IssueService.IntegrationTests.Fixtures;
using IssueService.IntegrationTests.Helpers;

namespace IssueService.IntegrationTests.Issues;

/// <summary>
/// Regression tests for the organization-header spoofing fix.
///
/// The controllers resolve the caller's organization from the <c>org_id</c> JWT claim
/// only. The <c>X-Organization-Id</c> request header is honoured exclusively by
/// InternalServiceMiddleware, and only when it arrives with a valid internal service
/// name and API key. An end user must therefore never be able to widen their scope by
/// attaching that header to their own authenticated request.
/// </summary>
public sealed class OrganizationScopeSecurityTests : IClassFixture<IssueWebAppFactory>
{
    private readonly IssueWebAppFactory _factory;

    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private readonly Guid _projectB = Guid.NewGuid();

    public OrganizationScopeSecurityTests(IssueWebAppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient ClientForOrgA()
    {
        var client = _factory.CreateClient();
        client.WithJwt(_userA, "attacker@org-a.test", organizationId: _orgA, organizationRole: "Owner");
        return client;
    }

    private HttpClient ClientForOrgB()
    {
        var client = _factory.CreateClient();
        client.WithJwt(_userB, "victim@org-b.test", organizationId: _orgB, organizationRole: "Owner");
        return client;
    }

    /// <summary>Creates an issue owned by organization B and returns its id.</summary>
    private async Task<Guid> SeedOrgBIssueAsync(string title)
    {
        var response = await ClientForOrgB().PostAsJsonAsync("/api/v1/issues", new
        {
            ProjectId = _projectB,
            Title = title,
            Description = (string?)null,
            Priority = (int)IssuePriority.Medium,
            CreatedByUserId = _userB,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IssueIdResponse>();
        body.Should().NotBeNull();
        return body!.Id;
    }

    [Fact]
    public async Task GetByProject_WithSpoofedOrgHeader_DoesNotLeakOtherOrgIssues()
    {
        await SeedOrgBIssueAsync("Org B confidential issue");

        // Control: org B's own token really can see the issue, so an empty result
        // below cannot be explained away by the data simply not being there.
        var victimResponse = await ClientForOrgB().GetAsync($"/api/v1/issues/project/{_projectB}");
        victimResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var victimIssues = await victimResponse.Content.ReadFromJsonAsync<List<IssueIdResponse>>();
        victimIssues.Should().NotBeNullOrEmpty("org B must be able to read its own project");

        // Attack: org A's token, with org B's id spoofed into the header.
        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/issues/project/{_projectB}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var leaked = await response.Content.ReadFromJsonAsync<List<IssueIdResponse>>();
        leaked.Should().NotBeNull();
        leaked!.Should().BeEmpty("a spoofed X-Organization-Id header must not widen the caller's scope");
    }

    [Fact]
    public async Task GetByProjectPaged_WithSpoofedOrgHeader_DoesNotLeakOtherOrgIssues()
    {
        await SeedOrgBIssueAsync("Org B paged issue");

        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/issues/project/{_projectB}/paged?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedIssueResponse>();
        page.Should().NotBeNull();
        page!.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetById_WithSpoofedOrgHeader_Returns403()
    {
        var issueId = await SeedOrgBIssueAsync("Org B single issue");

        // Control: the owner can read it.
        var victimResponse = await ClientForOrgB().GetAsync($"/api/v1/issues/{issueId}");
        victimResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/issues/{issueId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InternalServiceHeaders_WithoutValidApiKey_Are403()
    {
        // The header is only trusted for internal callers, and those must present a
        // valid API key. Forging the caller name alone must be rejected outright.
        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Internal-Service", "AiService");
        attacker.DefaultRequestHeaders.Add("X-Internal-Service-Key", "not-the-real-key");
        attacker.DefaultRequestHeaders.Add("X-User-Id", _userA.ToString());
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/issues/project/{_projectB}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class IssueIdResponse
    {
        public Guid Id { get; set; }
        public Guid IssueId { get; set; }
    }

    private sealed class PagedIssueResponse
    {
        public List<IssueIdResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
