using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Features.Organizations.Commands.CreateOrganization;
using BitirmeProject.IdentityService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace IdentityService.UnitTests.Application.Handlers;

public sealed class CreateOrganizationCommandHandlerTests
{
    private static (
        IOrganizationRepository orgRepo,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork,
        CreateOrganizationCommandHandler handler) CreateHandler()
    {
        var orgRepo = Substitute.For<IOrganizationRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreateOrganizationCommandHandler(orgRepo, userRepo, unitOfWork);
        return (orgRepo, userRepo, unitOfWork, handler);
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        var (orgRepo, userRepo, _, handler) = CreateHandler();

        userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new CreateOrganizationCommand("Acme Corp", Guid.NewGuid());
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
        await orgRepo.DidNotReceive().AddAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserAlreadyBelongsToOrganization()
    {
        var (orgRepo, userRepo, _, handler) = CreateHandler();

        var userId = Guid.NewGuid();
        var user = new User("alice", "alice@example.com", "hash");
        userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var existingOrg = new Organization("Existing Org", userId);
        orgRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(existingOrg);

        var command = new CreateOrganizationCommand("New Org", userId);
        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already belongs*");
    }

    [Fact]
    public async Task Handle_CreatesOrganization_AndReturnsDto()
    {
        var (orgRepo, userRepo, unitOfWork, handler) = CreateHandler();

        var userId = Guid.NewGuid();
        var user = new User("alice", "alice@example.com", "hash");
        userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        orgRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Organization?)null);

        var command = new CreateOrganizationCommand("Acme Corp", userId);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Acme Corp");
        result.CreatedByUserId.Should().Be(userId);
        result.MemberCount.Should().Be(1);
        await orgRepo.Received(1).AddAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
