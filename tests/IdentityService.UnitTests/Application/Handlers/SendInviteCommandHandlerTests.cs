using BitirmeProject.IdentityService.Application.Abstractions;
using AutoMapper;
using BitirmeProject.IdentityService.Application.Features.Invites.Commands.SendInvite;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace IdentityService.UnitTests.Application.Handlers;

public sealed class SendInviteCommandHandlerTests
{
    private static (
        IOrganizationRepository orgRepo,
        IInviteRepository inviteRepo,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        SendInviteCommandHandler handler) CreateHandler()
    {
        var orgRepo = Substitute.For<IOrganizationRepository>();
        var inviteRepo = Substitute.For<IInviteRepository>();
        var emailService = Substitute.For<IEmailService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<SendInviteCommandHandler>>();
        var handler = new SendInviteCommandHandler(orgRepo, inviteRepo, emailService, unitOfWork, mapper, logger);
        return (orgRepo, inviteRepo, emailService, unitOfWork, mapper, handler);
    }

    [Fact]
    public async Task Handle_Throws_WhenOrganizationNotFound()
    {
        var (orgRepo, _, _, _, _, handler) = CreateHandler();

        orgRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);

        var command = new SendInviteCommand(
            Guid.NewGuid(), Guid.NewGuid(), "new@example.com",
            OrganizationRole.Member, "http://localhost:5173");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_Throws_WhenRequesterIsNotMember()
    {
        var (orgRepo, _, _, _, _, handler) = CreateHandler();

        var ownerId = Guid.NewGuid();
        var org = new Organization("Acme", ownerId);
        orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var command = new SendInviteCommand(
            org.Id, Guid.NewGuid() /* stranger */, "new@example.com",
            OrganizationRole.Member, "http://localhost:5173");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_Throws_WhenRequesterIsMemberRole()
    {
        var (orgRepo, _, _, _, _, handler) = CreateHandler();

        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var org = new Organization("Acme", ownerId);
        org.AddMember(memberId, OrganizationRole.Member);
        orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var command = new SendInviteCommand(
            org.Id, memberId, "new@example.com",
            OrganizationRole.Member, "http://localhost:5173");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Owner or Manager*");
    }

    [Fact]
    public async Task Handle_Throws_WhenPendingInviteAlreadyExists()
    {
        var (orgRepo, inviteRepo, _, _, _, handler) = CreateHandler();

        var ownerId = Guid.NewGuid();
        var org = new Organization("Acme", ownerId);
        orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        inviteRepo.HasPendingInviteAsync("new@example.com", org.Id, Arg.Any<CancellationToken>()).Returns(true);

        var command = new SendInviteCommand(
            org.Id, ownerId, "new@example.com",
            OrganizationRole.Member, "http://localhost:5173");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pending invite*");
    }

    [Fact]
    public async Task Handle_CreatesInvite_SendsEmail_AndReturnsDto()
    {
        var (orgRepo, inviteRepo, emailService, unitOfWork, mapper, handler) = CreateHandler();

        var ownerId = Guid.NewGuid();
        var org = new Organization("Acme", ownerId);
        orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        inviteRepo.HasPendingInviteAsync(Arg.Any<string>(), org.Id, Arg.Any<CancellationToken>()).Returns(false);

        var command = new SendInviteCommand(
            org.Id, ownerId, "new@example.com",
            OrganizationRole.Member, "http://localhost:5173");

        mapper.Map<BitirmeProject.IdentityService.Application.DTOs.InviteDto>(Arg.Any<InviteToken>())
            .Returns(new BitirmeProject.IdentityService.Application.DTOs.InviteDto
            {
                Email = "new@example.com",
                Role = "Member"
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be("new@example.com");
        result.Role.Should().Be("Member");
        await inviteRepo.Received(1).AddAsync(Arg.Any<InviteToken>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await emailService.Received(1).SendInviteEmailAsync(
            "new@example.com",
            "Acme",
            Arg.Is<string>(s => s.Contains("invite/accept?token=")),
            Arg.Any<CancellationToken>());
    }
}
