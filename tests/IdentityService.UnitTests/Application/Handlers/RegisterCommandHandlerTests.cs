using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Auth.Commands.Register;
using BitirmeProject.IdentityService.Application.Options;
using BitirmeProject.IdentityService.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shared.Abstractions.Messaging;

namespace IdentityService.UnitTests.Application.Handlers;

public sealed class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenUserNameExists()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var outboxRepo = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        userRepository.ExistsByUserNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new RegisterCommandHandler(userRepository, hasher, unitOfWork, jwt, roleRepo, refreshRepo, outboxRepo, mapper, options);
        var command = new RegisterCommand("user", "user@example.com", "Pass123!");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Throws_WhenEmailExists()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var outboxRepo = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        userRepository.ExistsByUserNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new RegisterCommandHandler(userRepository, hasher, unitOfWork, jwt, roleRepo, refreshRepo, outboxRepo, mapper, options);
        var command = new RegisterCommand("user", "user@example.com", "Pass123!");

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_CreatesUser_AssignsDefaultRole_AndReturnsTokens()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var jwt = Substitute.For<IJwtTokenGenerator>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var outboxRepo = Substitute.For<IOutboxRepository>();
        var mapper = Substitute.For<IMapper>();
        var options = Options.Create(new JwtOptions { RefreshTokenDays = 7 });

        userRepository.ExistsByUserNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        hasher.HashPassword(Arg.Any<string>()).Returns("hashed_password");

        var defaultRole = new Role("Viewer");
        roleRepo.GetByNameAsync("Viewer", Arg.Any<CancellationToken>()).Returns(defaultRole);

        jwt.Generate(Arg.Any<User>(), Arg.Any<IReadOnlyList<string>>()).Returns(new JwtTokenResult("access", DateTime.UtcNow.AddHours(1)));
        mapper.Map<UserDto>(Arg.Any<User>()).Returns(new UserDto { Id = Guid.NewGuid(), Email = "user@example.com" });

        var handler = new RegisterCommandHandler(userRepository, hasher, unitOfWork, jwt, roleRepo, refreshRepo, outboxRepo, mapper, options);
        var command = new RegisterCommand("user", "user@example.com", "Pass123!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        await refreshRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
