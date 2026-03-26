using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;
using BitirmeProject.ProjectService.Domain.Entities;
using BitirmeProject.ProjectService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;
using Shared.Abstractions.Messaging;

namespace ProjectService.UnitTests.Application.Handlers;

public sealed class AddMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenProjectMissing()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var summaryRepository = Substitute.For<IProjectSummaryRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new AddMemberCommandHandler(projectRepository, summaryRepository, memberRepository, unitOfWork, outboxRepository, mapper);
        var command = new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, ProjectMemberRole.Member);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await memberRepository.DidNotReceive().AddAsync(Arg.Any<ProjectMember>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserAlreadyMember()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var summaryRepository = Substitute.For<IProjectSummaryRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new Project("Name", "KEY", Guid.NewGuid()));
        memberRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProjectMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Owner));
        memberRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new AddMemberCommandHandler(projectRepository, summaryRepository, memberRepository, unitOfWork, outboxRepository, mapper);
        var command = new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, ProjectMemberRole.Member);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await memberRepository.DidNotReceive().AddAsync(Arg.Any<ProjectMember>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddsMember_AndWritesOutbox()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var summaryRepository = Substitute.For<IProjectSummaryRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var project = new Project("Name", "KEY", Guid.NewGuid());
        projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(project);
        memberRepository.GetAsync(project.Id, project.OwnerUserId, Arg.Any<CancellationToken>())
            .Returns(new ProjectMember(project.Id, project.OwnerUserId, project.OwnerUserId, ProjectMemberRole.Owner));
        memberRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        summaryRepository.GetByProjectIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(new ProjectSummary(project.Id));

        var handler = new AddMemberCommandHandler(projectRepository, summaryRepository, memberRepository, unitOfWork, outboxRepository, mapper);
        var command = new AddMemberCommand(project.Id, Guid.NewGuid(), project.OwnerUserId, Guid.NewGuid(), ProjectMemberRole.Admin);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(project.Id);
        await memberRepository.Received(1).AddAsync(Arg.Any<ProjectMember>(), Arg.Any<CancellationToken>());
        await outboxRepository.Received(1).AddAsync(Arg.Is<OutboxMessage>(m =>
            m.EventType == "MemberAddedEvent" &&
            !string.IsNullOrWhiteSpace(m.Payload)), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenRequesterCannotManageMembers()
    {
        var projectRepository = Substitute.For<IProjectRepository>();
        var summaryRepository = Substitute.For<IProjectSummaryRepository>();
        var memberRepository = Substitute.For<IProjectMemberRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();

        var project = new Project("Name", "KEY", Guid.NewGuid());
        var memberUserId = Guid.NewGuid();
        projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        memberRepository.GetAsync(project.Id, memberUserId, Arg.Any<CancellationToken>())
            .Returns(new ProjectMember(project.Id, memberUserId, project.OwnerUserId, ProjectMemberRole.Member));

        var handler = new AddMemberCommandHandler(projectRepository, summaryRepository, memberRepository, unitOfWork, outboxRepository, mapper);
        var command = new AddMemberCommand(project.Id, Guid.NewGuid(), memberUserId, null, ProjectMemberRole.Member);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await memberRepository.DidNotReceive().AddAsync(Arg.Any<ProjectMember>(), Arg.Any<CancellationToken>());
        await outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }
}
