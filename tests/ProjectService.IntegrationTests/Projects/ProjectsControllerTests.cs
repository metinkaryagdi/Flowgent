using Xunit;
using System.Net;
using System.Net.Http.Json;
using BitirmeProject.ProjectService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectService.IntegrationTests.Fixtures;
using ProjectService.IntegrationTests.Helpers;

namespace ProjectService.IntegrationTests.Projects;

public sealed class ProjectsControllerTests : IClassFixture<ProjectWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly ProjectWebAppFactory _factory;

    private readonly Guid _userId = Guid.NewGuid();
    private const string UserEmail = "owner@test.com";

    public ProjectsControllerTests(ProjectWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.WithJwt(_userId, UserEmail);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201AndProject()
    {
        var request = new
        {
            Name = $"Test Project {Guid.NewGuid()}",
            Key = $"TP{Random.Shared.Next(100, 999)}",
            OwnerUserId = Guid.Empty,   // Controller bunu Claims'den alır
            CorrelationId = (Guid?)null
        };

        var response = await _client.PostAsJsonAsync("/api/v1/projects", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be(request.Name);
        body.Key.Should().Be(request.Key);
        body.OwnerUserId.Should().Be(_userId);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.PostAsJsonAsync("/api/v1/projects", new
        {
            Name = "No Auth Project",
            Key = "NAP"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingProject_Returns200()
    {
        // Önce oluştur
        var created = await CreateProjectAsync("GetById Test");
        created.Should().NotBeNull();

        var response = await _client.GetAsync($"/api/v1/projects/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonExisting_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ByOwner_Returns200()
    {
        var project = await CreateProjectAsync("Before Update");
        project.Should().NotBeNull();

        var response = await _client.PutAsJsonAsync($"/api/v1/projects/{project!.Id}", new
        {
            Id = project.Id,
            Name = "After Update",
            Key = project.Key,
            UpdatedByUserId = Guid.Empty,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        body!.Name.Should().Be("After Update");
    }

    [Fact]
    public async Task Update_ByNonOwner_Returns403()
    {
        var project = await CreateProjectAsync("Owner Project");
        project.Should().NotBeNull();

        // Farklı kullanıcı ile istek at
        var otherClient = _factory.CreateClient().WithJwt(Guid.NewGuid(), "other@test.com");
        var response = await otherClient.PutAsJsonAsync($"/api/v1/projects/{project!.Id}", new
        {
            Id = project.Id,
            Name = "Hijacked",
            Key = project.Key,
            UpdatedByUserId = Guid.Empty,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── Delete ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ByOwner_Returns200()
    {
        var project = await CreateProjectAsync("To Delete");
        project.Should().NotBeNull();

        var response = await _client.DeleteAsync($"/api/v1/projects/{project!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_ByNonOwner_Returns403()
    {
        var project = await CreateProjectAsync("Protected Project");
        project.Should().NotBeNull();

        var otherClient = _factory.CreateClient().WithJwt(Guid.NewGuid(), "hacker@test.com");
        var response = await otherClient.DeleteAsync($"/api/v1/projects/{project!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── Members ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMember_ValidRequest_Returns200()
    {
        var project = await CreateProjectAsync("Team Project");
        project.Should().NotBeNull();

        var memberId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/api/v1/projects/{project!.Id}/members", new
        {
            ProjectId = project.Id,
            UserId = memberId,
            Role = 2,   // ProjectMemberRole.Member
            AddedByUserId = Guid.Empty,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GetMembers ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMembers_ReturnsListForProject()
    {
        var project = await CreateProjectAsync("Member List Project");
        project.Should().NotBeNull();

        var response = await _client.GetAsync($"/api/v1/projects/{project!.Id}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<ProjectResponse?> CreateProjectAsync(string name)
    {
        var key = $"K{Random.Shared.Next(1000, 9999)}";
        var response = await _client.PostAsJsonAsync("/api/v1/projects", new
        {
            Name = name,
            Key = key,
            OwnerUserId = Guid.Empty,
            CorrelationId = (Guid?)null
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectResponse>();
    }

    private sealed class ProjectResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public Guid OwnerUserId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
