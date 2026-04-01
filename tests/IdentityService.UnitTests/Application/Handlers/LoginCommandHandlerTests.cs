using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Login;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IdentityService.UnitTests.Application.Handlers;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenUserMissing()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        userRepository.GetByUserNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var orgRepo = Substitute.For<IOrganizationRepository>();
        var handler = new LoginCommandHandler(userRepository, hasher, jwt, refreshRepo, orgRepo, unitOfWork, mapper, options);
        var command = new LoginCommand("user", "pass");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Throws_WhenPasswordInvalid()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        var user = new User("user", "user@example.com", "hash");
        userRepository.GetByUserNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        hasher.VerifyPassword(user.PasswordHash, Arg.Any<string>()).Returns(false);

        var orgRepo = Substitute.For<IOrganizationRepository>();
        var handler = new LoginCommandHandler(userRepository, hasher, jwt, refreshRepo, orgRepo, unitOfWork, mapper, options);
        var command = new LoginCommand("user", "pass");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ReturnsTokens_WhenValid()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        var user = new User("user", "user@example.com", "hash");
        userRepository.GetByUserNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        hasher.VerifyPassword(user.PasswordHash, Arg.Any<string>()).Returns(true);

        jwt.Generate(user, Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<string?>()).Returns(new JwtTokenResult("access", DateTime.UtcNow.AddHours(1)));
        mapper.Map<UserDto>(user).Returns(new UserDto { Id = user.Id, Email = user.Email });

        var orgRepo = Substitute.For<IOrganizationRepository>();
        var handler = new LoginCommandHandler(userRepository, hasher, jwt, refreshRepo, orgRepo, unitOfWork, mapper, options);
        var command = new LoginCommand("user", "pass");

        var result = await handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        await refreshRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
