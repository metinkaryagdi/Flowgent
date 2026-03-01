using BitirmeProject.IssueService.Api.Controllers;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.AssignIssue;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.ChangeIssueStatus;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shared.Abstractions.Exceptions;

namespace IssueService.UnitTests.Controllers;

public sealed class IssuesControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithResult()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new IssuesController(mediator);
        var command = new CreateIssueCommand(Guid.NewGuid(), "Title", null, IssuePriority.Medium, Guid.NewGuid(), null);
        var dto = new IssueDto { Id = Guid.NewGuid(), ProjectId = command.ProjectId, Title = command.Title };
        mediator.Send(command).Returns(dto);

        var result = await controller.Create(command);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(IssuesController.GetById));
        created.RouteValues.Should().ContainKey("id");
        created.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new IssuesController(mediator);
        var id = Guid.NewGuid();
        mediator.Send(Arg.Any<BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById.GetIssueByIdQuery>())
            .Returns((IssueDto?)null);

        var result = await controller.GetById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Assign_UsesExpectedVersion_FromBody_WhenProvided()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new IssuesController(mediator);
        var issueId = Guid.NewGuid();
        var command = new AssignIssueCommand(issueId, Guid.NewGuid(), Guid.NewGuid(), 5, null);
        var dto = new IssueDto { Id = issueId };
        mediator.Send(Arg.Any<AssignIssueCommand>()).Returns(dto);

        var result = await controller.Assign(issueId, command, null, null);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<AssignIssueCommand>(c =>
            c.IssueId == issueId &&
            c.ExpectedVersion == 5));
    }

    [Fact]
    public async Task Assign_UsesExpectedVersion_FromHeader_WhenBodyMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new IssuesController(mediator);
        var issueId = Guid.NewGuid();
        var command = new AssignIssueCommand(issueId, Guid.NewGuid(), Guid.NewGuid(), 0, null);
        var dto = new IssueDto { Id = issueId };
        mediator.Send(Arg.Any<AssignIssueCommand>()).Returns(dto);

        var result = await controller.Assign(issueId, command, null, "7");

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<AssignIssueCommand>(c =>
            c.IssueId == issueId &&
            c.ExpectedVersion == 7));
    }

    [Fact]
    public async Task Assign_Throws_WhenExpectedVersionMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new IssuesController(mediator);
        var issueId = Guid.NewGuid();
        var command = new AssignIssueCommand(issueId, Guid.NewGuid(), Guid.NewGuid(), 0, null);

        var act = async () => await controller.Assign(issueId, command, null, null);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ChangeStatus_UsesExpectedVersion_FromIfMatch()
    {
        var mediator = Substitute.For<IMediator>();
        var controller = new IssuesController(mediator);
        var issueId = Guid.NewGuid();
        var command = new ChangeIssueStatusCommand(issueId, IssueStatus.InProgress, Guid.NewGuid(), 0, null);
        var dto = new IssueDto { Id = issueId };
        mediator.Send(Arg.Any<ChangeIssueStatusCommand>()).Returns(dto);

        var result = await controller.ChangeStatus(issueId, command, "\"9\"", null);

        result.Result.Should().BeOfType<OkObjectResult>();
        await mediator.Received(1).Send(Arg.Is<ChangeIssueStatusCommand>(c =>
            c.IssueId == issueId &&
            c.ExpectedVersion == 9));
    }
}
