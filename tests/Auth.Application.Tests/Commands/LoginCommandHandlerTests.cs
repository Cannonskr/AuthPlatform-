using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Application.Features.Auth.Commands;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;

namespace Auth.Application.Tests.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _contextMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenServiceMock.Object);
    }

    private static User CreateTestUser()
    {
        return new User(
            Guid.Parse("e5f6a7b8-c9d0-1234-ef12-345678901234"),
            "admin",
            "admin@test.com",
            "hashed-password",
            "System",
            "Admin");
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessResult()
    {
        var user = CreateTestUser();
        var users = new List<User> { user };

        _contextMock.Setup(x => x.Users).ReturnsDbSet(users);
        _passwordHasherMock.Setup(x => x.Verify("Password@123", "hashed-password")).Returns(true);
        _jwtServiceMock
            .Setup(x => x.GenerateTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new JwtToken { AccessToken = "jwt-token", ExpiresAt = DateTime.UtcNow.AddMinutes(15), TokenId = "tid" });
        _refreshTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("refresh-token");

        var result = await _handler.Handle(
            new LoginCommand("admin@test.com", "Password@123", "admin-portal"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("jwt-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsFailedResult()
    {
        _contextMock.Setup(x => x.Users).ReturnsDbSet(new List<User>());

        var result = await _handler.Handle(
            new LoginCommand("unknown@test.com", "Pass@123", "admin-portal"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailedAndRecordsAttempt()
    {
        var user = CreateTestUser();
        _contextMock.Setup(x => x.Users).ReturnsDbSet(new List<User> { user });
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await _handler.Handle(
            new LoginCommand("admin@test.com", "WrongPass@123", "admin-portal"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
        user.AccessFailedCount.Should().Be(1);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_LockedAccount_ReturnsFailed()
    {
        var user = CreateTestUser();
        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);

        _contextMock.Setup(x => x.Users).ReturnsDbSet(new List<User> { user });
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await _handler.Handle(
            new LoginCommand("admin@test.com", "Pass@123", "admin-portal"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Account is locked or inactive.");
    }

    [Fact]
    public async Task Handle_InactiveAccount_ReturnsFailed()
    {
        var user = CreateTestUser();
        user.Deactivate();

        _contextMock.Setup(x => x.Users).ReturnsDbSet(new List<User> { user });
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await _handler.Handle(
            new LoginCommand("admin@test.com", "Pass@123", "admin-portal"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Account is locked or inactive.");
    }
}
