using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.Common;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using BitirmeProject.IdentityService.Domain.Entities;
using BitirmeProject.IdentityService.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Messaging;

namespace IdentityService.UnitTests.Application.Handlers;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly IEmailVerificationIssuer _issuer = Substitute.For<IEmailVerificationIssuer>();

    private RegisterCommandHandler CreateHandler() =>
        new(_userRepository, _hasher, _unitOfWork, _roleRepo, _outboxRepo, _issuer);

    [Fact]
    public async Task Handle_Throws_WhenUserNameExists()
    {
        _userRepository.ExistsByUserNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = async () => await CreateHandler().Handle(
            new RegisterCommand("user", "user@example.com", "Pass123!"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Throws_WhenEmailExists()
    {
        _userRepository.ExistsByUserNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = async () => await CreateHandler().Handle(
            new RegisterCommand("user", "user@example.com", "Pass123!"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_CreatesPendingUser_AssignsDefaultRole_AndSendsVerification()
    {
        ArrangeHappyPath();

        User? persisted = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => persisted = u), Arg.Any<CancellationToken>());

        var result = await CreateHandler().Handle(
            new RegisterCommand("user", "user@example.com", "Pass123!"), CancellationToken.None);

        result.VerificationRequired.Should().BeTrue();
        result.Email.Should().Be("user@example.com");

        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(UserStatus.Pending);
        persisted.IsEmailVerified.Should().BeFalse();
        persisted.UserRoles.Should().ContainSingle();

        await _issuer.Received(1).IssueAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The whole point of the change: registration must not hand back credentials, so an
    /// unverified address can never reach an authenticated endpoint. RegisterResponseDto
    /// has no token properties at all, so this is enforced by the type -- the assertion
    /// below guards the shape of that DTO against someone adding them back.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsNoTokens()
    {
        ArrangeHappyPath();

        var result = await CreateHandler().Handle(
            new RegisterCommand("user", "user@example.com", "Pass123!"), CancellationToken.None);

        result.GetType().GetProperties()
            .Select(p => p.Name)
            .Should().NotContain(new[] { "AccessToken", "RefreshToken" });
    }

    private void ArrangeHappyPath()
    {
        _userRepository.ExistsByUserNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.HashPassword(Arg.Any<string>()).Returns("hashed_password");
        _roleRepo.GetByNameAsync("Member", Arg.Any<CancellationToken>()).Returns(new Role("Member"));
    }
}
