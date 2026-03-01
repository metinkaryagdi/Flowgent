using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Shared.Abstractions.Exceptions;

namespace IssueService.UnitTests.Domain;

public sealed class IssueWorkflowTests
{
    [Fact]
    public void IsTransitionAllowed_ReturnsTrue_ForConfiguredTransitions()
    {
        BitirmeProject.IssueService.Domain.Workflow.IssueWorkflow.IsTransitionAllowed(IssueStatus.Open, IssueStatus.InProgress).Should().BeTrue();
        BitirmeProject.IssueService.Domain.Workflow.IssueWorkflow.IsTransitionAllowed(IssueStatus.InProgress, IssueStatus.Done).Should().BeTrue();
        BitirmeProject.IssueService.Domain.Workflow.IssueWorkflow.IsTransitionAllowed(IssueStatus.Done, IssueStatus.InProgress).Should().BeTrue();
    }

    [Fact]
    public void IsTransitionAllowed_ReturnsFalse_ForInvalidTransitions()
    {
        BitirmeProject.IssueService.Domain.Workflow.IssueWorkflow.IsTransitionAllowed(IssueStatus.Open, IssueStatus.Done).Should().BeFalse();
        BitirmeProject.IssueService.Domain.Workflow.IssueWorkflow.IsTransitionAllowed(IssueStatus.InProgress, IssueStatus.Open).Should().BeFalse();
    }
}
