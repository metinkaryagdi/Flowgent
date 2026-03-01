using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Refresh;
using BitirmeProject.IdentityService.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IdentityService.UnitTests.Application.Handlers;

public sealed class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenTokenInvalid()
    {
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        refreshRepo.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var handler = new RefreshTokenCommandHandler(refreshRepo, userRepo, jwt, unitOfWork, mapper, options);
        var command = new RefreshTokenCommand("bad");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Throws_WhenUserInactive()
    {
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        var token = new RefreshToken(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(1));
        refreshRepo.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        var user = new User("user", "user@example.com", "hash");
        user.Deactivate();
        userRepo.GetByIdAsync(token.UserId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new RefreshTokenCommandHandler(refreshRepo, userRepo, jwt, unitOfWork, mapper, options);
        var command = new RefreshTokenCommand("token");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ReplacesToken_AndReturnsNewTokens()
    {
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        var token = new RefreshToken(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(1));
        refreshRepo.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        var user = new User("user", "user@example.com", "hash");
        userRepo.GetByIdAsync(token.UserId, Arg.Any<CancellationToken>()).Returns(user);

        jwt.Generate(user, Arg.Any<IReadOnlyList<string>>()).Returns(new JwtToken("access", DateTime.UtcNow.AddHours(1)));
        mapper.Map<UserDto>(user).Returns(new UserDto { Id = user.Id, Email = user.Email });

        var handler = new RefreshTokenCommandHandler(refreshRepo, userRepo, jwt, unitOfWork, mapper, options);
        var command = new RefreshTokenCommand("token");

        var result = await handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        await refreshRepo.Received(1).UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await refreshRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
