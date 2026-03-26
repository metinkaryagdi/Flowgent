using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace ProjectService.UnitTests.Application.Handlers;

public sealed class CreateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenKeyExists()
    {
        var repository = Substitute.For<IProjectRepository>();
        var summaryRepository = Substitute.For<IProjectSummaryRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.ExistsByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateProjectCommandHandler(repository, summaryRepository, memberRepository, unitOfWork, outboxRepository, mapper);
        var command = new CreateProjectCommand("Name", "KEY", Guid.NewGuid(), null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await repository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesProject_AndOutbox()
    {
        var repository = Substitute.For<IProjectRepository>();
        var summaryRepository = Substitute.For<IProjectSummaryRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.ExistsByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Project? capturedProject = null;
        repository.AddAsync(Arg.Do<Project>(x => capturedProject = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ProjectMember? capturedOwnerMember = null;
        memberRepository.AddAsync(Arg.Do<ProjectMember>(x => capturedOwnerMember = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new CreateProjectCommandHandler(repository, summaryRepository, memberRepository, unitOfWork, outboxRepository, mapper);
        var command = new CreateProjectCommand("Name", "KEY", Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        capturedProject.Should().NotBeNull();
        result.Id.Should().Be(capturedProject!.Id);
        capturedProject!.Name.Should().Be(command.Name);
        capturedProject.Key.Should().Be(command.Key);
        await summaryRepository.Received(1).AddAsync(Arg.Is<ProjectSummary>(s => s.ProjectId == capturedProject.Id), Arg.Any<CancellationToken>());
        capturedOwnerMember.Should().NotBeNull();
        capturedOwnerMember!.ProjectId.Should().Be(capturedProject.Id);
        capturedOwnerMember.UserId.Should().Be(command.OwnerUserId);
        capturedOwnerMember.Role.Should().Be(ProjectMemberRole.Owner);

        await repository.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "ProjectCreatedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
