using Xunit;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SprintService.IntegrationTests.Fixtures;
using SprintService.IntegrationTests.Helpers;

namespace SprintService.IntegrationTests.Sprints;

/// <summary>
/// Regression tests for the organization-header spoofing fix.
///
/// SprintsController resolves the caller's organization from the <c>org_id</c> JWT
/// claim only. The <c>X-Organization-Id</c> header is honoured exclusively by
/// InternalServiceMiddleware, and only alongside a valid internal API key, so an end
/// user must not be able to reach another organization's sprints by attaching it.
/// </summary>
public sealed class OrganizationScopeSecurityTests : IClassFixture<SprintWebAppFactory>
{
    private readonly SprintWebAppFactory _factory;

    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private readonly Guid _projectB = Guid.NewGuid();

    public OrganizationScopeSecurityTests(SprintWebAppFactory factory)
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

    /// <summary>Creates a sprint owned by organization B and returns its id.</summary>
    private async Task<Guid> SeedOrgBSprintAsync(string name)
    {
        var response = await ClientForOrgB().PostAsJsonAsync("/api/v1/sprints", new
        {
            ProjectId = _projectB,
            Name = name,
            Goal = "Org B internal goal",
            CreatedByUserId = _userB,
            CorrelationId = (Guid?)null,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(15)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SprintIdResponse>();
        body.Should().NotBeNull();
        return body!.Id;
    }

    [Fact]
    public async Task GetByProject_WithSpoofedOrgHeader_DoesNotLeakOtherOrgSprints()
    {
        await SeedOrgBSprintAsync("Org B Sprint");

        // Control: org B really can see its own sprint, so an empty result below
        // cannot be explained by the data simply not existing.
        var victimResponse = await ClientForOrgB().GetAsync($"/api/v1/sprints/project/{_projectB}");
        victimResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var victimSprints = await victimResponse.Content.ReadFromJsonAsync<List<SprintIdResponse>>();
        victimSprints.Should().NotBeNullOrEmpty("org B must be able to read its own project");

        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/sprints/project/{_projectB}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var leaked = await response.Content.ReadFromJsonAsync<List<SprintIdResponse>>();
        leaked.Should().NotBeNull();
        leaked!.Should().BeEmpty("a spoofed X-Organization-Id header must not widen the caller's scope");
    }

    [Fact]
    public async Task GetById_WithSpoofedOrgHeader_Returns403()
    {
        var sprintId = await SeedOrgBSprintAsync("Org B Single Sprint");

        // Control: the owner can read it.
        var victimResponse = await ClientForOrgB().GetAsync($"/api/v1/sprints/{sprintId}");
        victimResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/sprints/{sprintId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetActive_WithSpoofedOrgHeader_DoesNotLeakOtherOrgSprint()
    {
        var sprintId = await SeedOrgBSprintAsync("Org B Active Sprint");
        var startResponse = await ClientForOrgB().PostAsync($"/api/v1/sprints/{sprintId}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Control: org B sees its active sprint (200 with a body).
        var victimResponse = await ClientForOrgB().GetAsync($"/api/v1/sprints/project/{_projectB}/active");
        victimResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var victimSprint = await victimResponse.Content.ReadFromJsonAsync<SprintIdResponse>();
        victimSprint.Should().NotBeNull();
        victimSprint!.Id.Should().Be(sprintId);

        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/sprints/project/{_projectB}/active");

        // A null result serialises to 204 NoContent, which is the "nothing to see"
        // answer. Either way the body must not carry org B's sprint.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().NotContain(sprintId.ToString(), "org A must not see org B's active sprint");
    }

    [Fact]
    public async Task InternalServiceHeaders_WithoutValidApiKey_Are403()
    {
        var attacker = ClientForOrgA();
        attacker.DefaultRequestHeaders.Add("X-Internal-Service", "AiService");
        attacker.DefaultRequestHeaders.Add("X-Internal-Service-Key", "not-the-real-key");
        attacker.DefaultRequestHeaders.Add("X-User-Id", _userA.ToString());
        attacker.DefaultRequestHeaders.Add("X-Organization-Id", _orgB.ToString());

        var response = await attacker.GetAsync($"/api/v1/sprints/project/{_projectB}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class SprintIdResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
