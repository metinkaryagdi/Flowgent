using Xunit;
using System.Net;
using System.Net.Http.Json;
using BitirmeProject.SprintService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintService.IntegrationTests.Fixtures;
using SprintService.IntegrationTests.Helpers;

namespace SprintService.IntegrationTests.Sprints;

public sealed class SprintsControllerTests : IClassFixture<SprintWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly SprintWebAppFactory _factory;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private const string UserEmail = "scrum@test.com";

    public SprintsControllerTests(SprintWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.WithJwt(_userId, UserEmail);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns200AndSprint()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sprints", new
        {
            ProjectId = _projectId,
            Name = "Sprint 1",
            Goal = "Deliver login feature",
            CreatedByUserId = _userId,
            CorrelationId = (Guid?)null,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(15)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SprintResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Sprint 1");
        body.ProjectId.Should().Be(_projectId);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.PostAsJsonAsync("/api/v1/sprints", new
        {
            ProjectId = _projectId,
            Name = "No Auth Sprint",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Start ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Start_ValidSprint_Returns200()
    {
        var sprint = await CreateSprintAsync("Sprint to Start");
        sprint.Should().NotBeNull();

        var response = await _client.PostAsJsonAsync($"/api/v1/sprints/{sprint!.Id}/start", new
        {
            SprintId = sprint.Id,
            StartedByUserId = _userId,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SprintResponse>();
        body!.Status.Should().Be("Active");
    }

    // ─── Complete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Complete_ActiveSprint_Returns200()
    {
        var sprint = await CreateSprintAsync("Sprint to Complete");
        sprint.Should().NotBeNull();

        // Önce başlat
        await _client.PostAsJsonAsync($"/api/v1/sprints/{sprint!.Id}/start", new
        {
            SprintId = sprint.Id,
            StartedByUserId = _userId,
            CorrelationId = (Guid?)null
        });

        // Tamamla
        var response = await _client.PostAsJsonAsync($"/api/v1/sprints/{sprint.Id}/complete", new
        {
            SprintId = sprint.Id,
            CompletedByUserId = _userId,
            CorrelationId = (Guid?)null,
            CarryOverPolicy = 0, // Backlog
            NextSprintId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SprintResponse>();
        body!.Status.Should().Be("Completed");
    }

    // ─── GetActive ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActive_NoActiveSprint_ReturnsNull()
    {
        var uniqueProjectId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/sprints/project/{uniqueProjectId}/active");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        // Aktif sprint yoksa null / boş body ya da 204 döner
    }

    [Fact]
    public async Task GetActive_WithActiveSprint_ReturnsSprint()
    {
        var sprint = await CreateSprintAsync("Active Sprint Query Test");
        sprint.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/v1/sprints/{sprint!.Id}/start", new
        {
            SprintId = sprint.Id,
            StartedByUserId = _userId,
            CorrelationId = (Guid?)null
        });

        var response = await _client.GetAsync($"/api/v1/sprints/project/{_projectId}/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SprintResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Active");
    }

    // ─── GetBacklog ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBacklog_Returns200()
    {
        var response = await _client.GetAsync($"/api/v1/sprints/project/{_projectId}/backlog");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GetIssues ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssues_Returns200ForExistingSprint()
    {
        var sprint = await CreateSprintAsync("Issues Sprint");
        sprint.Should().NotBeNull();

        var response = await _client.GetAsync($"/api/v1/sprints/{sprint!.Id}/issues");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<SprintResponse?> CreateSprintAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sprints", new
        {
            ProjectId = _projectId,
            Name = name,
            Goal = (string?)null,
            CreatedByUserId = _userId,
            CorrelationId = (Guid?)null,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(15)
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SprintResponse>();
    }

    private sealed class SprintResponse
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Goal { get; set; }
    }
}
