using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.Features.Issues.Commands.CreateIssue;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Abstractions.Messaging;

namespace IssueService.UnitTests.Application.Handlers;

public sealed class CreateIssueCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesIssue_AndPersistsOutbox()
    {
        var repository = Substitute.For<IIssueRepository>();
        var boardRepository = Substitute.For<IIssueBoardRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<CreateIssueCommandHandler>>();

        Issue? capturedIssue = null;
        repository.AddAsync(Arg.Do<Issue>(x => capturedIssue = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var expectedDto = new IssueDto { Id = Guid.NewGuid() };
        mapper.Map<IssueDto>(Arg.Any<Issue>()).Returns(expectedDto);

        var handler = new CreateIssueCommandHandler(repository, boardRepository, unitOfWork, outboxRepository, mapper, logger);
        var command = new CreateIssueCommand(Guid.NewGuid(), "Test Issue", "Desc", IssuePriority.High, Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        capturedIssue.Should().NotBeNull();
        capturedIssue!.ProjectId.Should().Be(command.ProjectId);
        capturedIssue.Title.Should().Be(command.Title);
        capturedIssue.Priority.Should().Be(command.Priority);

        await repository.Received(1).AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
        await boardRepository.Received(1).AddAsync(Arg.Any<IssueBoardItem>(), Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "IssueCreatedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<IssueDto>(Arg.Any<Issue>());
    }
}
