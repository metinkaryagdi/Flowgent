using BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueWorkflowConfig;
using BitirmeProject.IssueService.Domain.Workflow;
using FluentAssertions;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class GetIssueWorkflowConfigQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsStatuses_AndTransitions()
    {
        var handler = new GetIssueWorkflowConfigQueryHandler();
        var result = await handler.Handle(new GetIssueWorkflowConfigQuery(), CancellationToken.None);

        result.Statuses.Should().NotBeNull();
        result.Statuses.Should().HaveCount(IssueWorkflow.Statuses.Count);
        result.AllowedTransitions.Should().NotBeNull();
        result.AllowedTransitions.Should().ContainKey("Open");
    }
}
