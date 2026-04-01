using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Features.Invites.Commands.AcceptInvite;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace IdentityService.UnitTests.Application.Handlers;

public sealed class AcceptInviteCommandHandlerTests
{
    private static (
        IInviteRepository inviteRepo,
        IOrganizationRepository orgRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        AcceptInviteCommandHandler handler) CreateHandler()
    {
        var inviteRepo = Substitute.For<IInviteRepository>();
        var orgRepo = Substitute.For<IOrganizationRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AcceptInviteCommandHandler(inviteRepo, orgRepo, userRepo, roleRepo, hasher, unitOfWork);
        return (inviteRepo, orgRepo, userRepo, roleRepo, hasher, unitOfWork, handler);
    }

    [Fact]
    public async Task Handle_Throws_WhenTokenNotFound()
    {
        var (inviteRepo, _, _, _, _, _, handler) = CreateHandler();

        inviteRepo.GetByTokenAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((InviteToken?)null);

        var command = new AcceptInviteCommand(Guid.NewGuid(), "newuser", "Pass123!");
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_Throws_WhenTokenIsUsed()
    {
        var (inviteRepo, orgRepo, _, _, _, _, handler) = CreateHandler();

        var ownerId = Guid.NewGuid();
        var org = new Organization("Acme", ownerId);
        var invite = new InviteToken("used@example.com", org.Id, ownerId, OrganizationRole.Member);

        // Mark it as used
        org.AddMember(Guid.NewGuid(), OrganizationRole.Member); // dummy
        orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        // Simulate used token by creating a new one and calling MarkAsUsed
        var usedInvite = new InviteToken("used@example.com", org.Id, ownerId, OrganizationRole.Member);
        usedInvite.MarkAsUsed();

        inviteRepo.GetByTokenAsync(usedInvite.Token, Arg.Any<CancellationToken>()).Returns(usedInvite);

        var command = new AcceptInviteCommand(usedInvite.Token, "newuser", "Pass123!");
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired or already used*");
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNameAlreadyExists()
    {
        var (inviteRepo, orgRepo, userRepo, _, _, _, handler) = CreateHandler();

        var ownerId = Guid.NewGuid();
        var org = new Organization("Acme", ownerId);
        orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var invite = new InviteToken("new@example.com", org.Id, ownerId, OrganizationRole.Member);
        inviteRepo.GetByTokenAsync(invite.Token, Arg.Any<CancellationToken>()).Returns(invite);

        userRepo.ExistsByUserNameAsync("existinguser", null, Arg.Any<CancellationToken>()).Returns(true);

        var command = new AcceptInviteCommand(invite.Token, "existinguser", "Pass123!");
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_CreatesUserAndJoinsOrg_WhenTokenIsValid()
    {
        var (inviteRepo, orgRepo, userRepo, roleRepo, hasher, unitOfWork, handler) = CreateHandler();

        var ownerId = Guid.NewGuid();
        var org = new Organization("Acme", ownerId);
        orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);

        var invite = new InviteToken("new@example.com", org.Id, ownerId, OrganizationRole.Member);
        inviteRepo.GetByTokenAsync(invite.Token, Arg.Any<CancellationToken>()).Returns(invite);

        userRepo.ExistsByUserNameAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);
        userRepo.ExistsByEmailAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);
        hasher.HashPassword(Arg.Any<string>()).Returns("hashed");

        var defaultRole = new Role("Member");
        roleRepo.GetByNameAsync("Member", Arg.Any<CancellationToken>()).Returns(defaultRole);

        var command = new AcceptInviteCommand(invite.Token, "newuser", "Pass123!");
        var result = await handler.Handle(command, CancellationToken.None);

        result.UserName.Should().Be("newuser");
        result.Email.Should().Be("new@example.com");

        await userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await orgRepo.Received(1).UpdateAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>());
        await inviteRepo.Received(1).UpdateAsync(Arg.Any<InviteToken>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // Org should now have 2 members (owner + new)
        org.Members.Should().HaveCount(2);
    }
}
