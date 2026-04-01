using Xunit;
using System.Net;
using System.Net.Http.Json;
using BitirmeProject.IssueService.Domain.Enums;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using FluentAssertions;
using IssueService.IntegrationTests.Fixtures;
using IssueService.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IssueService.IntegrationTests.Issues;

public sealed class IssuesControllerTests : IClassFixture<IssueWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly IssueWebAppFactory _factory;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private const string UserEmail = "dev@test.com";

    public IssuesControllerTests(IssueWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.WithJwt(_userId, UserEmail);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidData_Returns201AndIssue()
    {
        var request = new
        {
            ProjectId = _projectId,
            Title = "Fix login bug",
            Description = "Users cannot login with SSO",
            Priority = (int)IssuePriority.High,
            CreatedByUserId = _userId,
            CorrelationId = (Guid?)null
        };

        var response = await _client.PostAsJsonAsync("/api/v1/issues", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IssueResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("Fix login bug");
        body.Status.Should().Be("Open");
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.PostAsJsonAsync("/api/v1/issues", new
        {
            ProjectId = _projectId,
            Title = "No auth issue",
            Priority = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingIssue_Returns200()
    {
        var issue = await CreateIssueAsync("Get by ID test");

        var response = await _client.GetAsync($"/api/v1/issues/{issue!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IssueResponse>();
        body!.Id.Should().Be(issue.Id);
    }

    [Fact]
    public async Task GetById_NonExisting_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/issues/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── GetByProject ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByProject_ReturnsIssuesList()
    {
        await CreateIssueAsync("Issue for project list");

        var response = await _client.GetAsync($"/api/v1/issues/project/{_projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<IssueResponse>>();
        body.Should().NotBeNull();
        body!.Count.Should().BeGreaterThan(0);
    }

    // ─── ChangeStatus ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_ToInProgress_Returns200()
    {
        var issue = await CreateIssueAsync("Status change test");
        issue.Should().NotBeNull();

        var response = await _client.PostAsJsonAsync($"/api/v1/issues/{issue!.Id}/status", new
        {
            IssueId = issue.Id,
            NewStatus = (int)IssueStatus.InProgress,
            ChangedByUserId = _userId,
            ExpectedVersion = issue.Version,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IssueResponse>();
        body!.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task ChangeStatus_ToDone_Returns200()
    {
        var issue = await CreateIssueAsync("Done status test");
        issue.Should().NotBeNull();

        // Open → InProgress first (workflow requires this step)
        var inProgressResponse = await _client.PostAsJsonAsync($"/api/v1/issues/{issue!.Id}/status", new
        {
            IssueId = issue.Id,
            NewStatus = (int)IssueStatus.InProgress,
            ChangedByUserId = _userId,
            ExpectedVersion = issue.Version,
            CorrelationId = (Guid?)null
        });
        inProgressResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inProgress = await inProgressResponse.Content.ReadFromJsonAsync<IssueResponse>();

        // InProgress → Done
        var response = await _client.PostAsJsonAsync($"/api/v1/issues/{issue.Id}/status", new
        {
            IssueId = issue.Id,
            NewStatus = (int)IssueStatus.Done,
            ChangedByUserId = _userId,
            ExpectedVersion = inProgress!.Version,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Assign ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Assign_ValidUser_Returns200()
    {
        var issue = await CreateIssueAsync("Assign test");
        issue.Should().NotBeNull();

        var assigneeId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/api/v1/issues/{issue!.Id}/assign", new
        {
            IssueId = issue.Id,
            AssigneeUserId = assigneeId,
            AssignedByUserId = _userId,
            ExpectedVersion = issue.Version,
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IssueResponse>();
        body!.AssigneeUserId.Should().Be(assigneeId);
    }

    // ─── AddComment ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddComment_ValidContent_Returns200()
    {
        var issue = await CreateIssueAsync("Comment test");
        issue.Should().NotBeNull();

        var response = await _client.PostAsJsonAsync($"/api/v1/issues/{issue!.Id}/comments", new
        {
            IssueId = issue.Id,
            AuthorUserId = _userId,
            Content = "This is a test comment",
            CorrelationId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GetHistory ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_ReturnsAuditList()
    {
        var issue = await CreateIssueAsync("History test");
        issue.Should().NotBeNull();

        var response = await _client.GetAsync($"/api/v1/issues/{issue!.Id}/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Workflow ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWorkflow_Returns200WithTransitions()
    {
        var response = await _client.GetAsync("/api/v1/issues/workflow");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<IssueResponse?> CreateIssueAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/issues", new
        {
            ProjectId = _projectId,
            Title = title,
            Description = (string?)null,
            Priority = (int)IssuePriority.Medium,
            CreatedByUserId = _userId,
            CorrelationId = (Guid?)null
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IssueResponse>();
    }

    private sealed class IssueResponse
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;   // IssueService enums as strings
        public string Priority { get; set; } = string.Empty;
        public Guid? AssigneeUserId { get; set; }
        public int Version { get; set; }
    }
}
