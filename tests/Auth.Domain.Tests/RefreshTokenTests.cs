using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class RefreshTokenTests
{
    [Fact]
    public void CreateRefreshToken_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = new RefreshToken("token123", "jwt123", userId, expiresAt, "portal", "Chrome", "127.0.0.1");

        token.Token.Should().Be("token123");
        token.JwtId.Should().Be("jwt123");
        token.UserId.Should().Be(userId);
        token.ExpiresAt.Should().Be(expiresAt);
        token.ApplicationCode.Should().Be("portal");
        token.DeviceInfo.Should().Be("Chrome");
        token.IpAddress.Should().Be("127.0.0.1");
        token.IsRevoked.Should().BeFalse();
        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnTrue_WhenNotRevokedNotUsedNotExpired()
    {
        var token = new RefreshToken("tok", "jwt", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenUsed()
    {
        var token = new RefreshToken("tok", "jwt", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        token.MarkAsUsed();

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenRevoked()
    {
        var token = new RefreshToken("tok", "jwt", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        token.Revoke();

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenExpired()
    {
        var token = new RefreshToken("tok", "jwt", Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_ShouldSetIsUsed()
    {
        var token = new RefreshToken("tok", "jwt", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        token.MarkAsUsed();

        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void Revoke_ShouldSetIsRevokedAndRevokedAt()
    {
        var token = new RefreshToken("tok", "jwt", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
