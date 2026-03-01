using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Shared.Abstractions.Exceptions;

namespace IssueService.UnitTests.Domain;

public sealed class IssueTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var issue = new Issue(Guid.NewGuid(), "Title", "Desc", IssuePriority.Medium, Guid.NewGuid());

        issue.Status.Should().Be(IssueStatus.Open);
        issue.Version.Should().Be(1);
        issue.Title.Should().Be("Title");
    }

    [Fact]
    public void AssignTo_IsIdempotent()
    {
        var assignee = Guid.NewGuid();
        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());
        var version = issue.Version;

        issue.AssignTo(assignee);
        var afterFirst = issue.Version;
        issue.AssignTo(assignee);

        issue.AssigneeUserId.Should().Be(assignee);
        issue.Version.Should().Be(afterFirst);
        issue.Version.Should().BeGreaterThan(version);
    }

    [Fact]
    public void ChangeStatus_Throws_WhenInvalidTransition()
    {
        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());

        var act = () => issue.ChangeStatus(IssueStatus.Done);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ChangeStatus_Updates_WhenValidTransition()
    {
        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());

        issue.ChangeStatus(IssueStatus.InProgress);

        issue.Status.Should().Be(IssueStatus.InProgress);
    }

    [Fact]
    public void SetTitle_Throws_WhenEmpty()
    {
        var issue = new Issue(Guid.NewGuid(), "Title", null, IssuePriority.Low, Guid.NewGuid());

        var act = () => issue.SetTitle(" ");

        act.Should().Throw<ArgumentException>();
    }
}
