using System.Security.Claims;
using Auth.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void IsAuthenticated_NoHttpContext_ReturnsFalse()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = null
        };
        var service = new CurrentUserService(httpContextAccessor);

        service.IsAuthenticated.Should().BeFalse();
        service.UserId.Should().BeNull();
        service.Email.Should().BeNull();
        service.Roles.Should().BeEmpty();
        service.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void UserId_WithSubClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid().ToString();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, "user@test.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("permission", "users.read"),
            new Claim("app", "admin-portal")
        }, "test-auth");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        service.IsAuthenticated.Should().BeTrue();
        service.UserId.Should().Be(Guid.Parse(userId));
        service.Email.Should().Be("user@test.com");
        service.FullName.Should().Be("Test User");
        service.Roles.Should().Contain("Admin");
        service.Permissions.Should().Contain("users.read");
        service.ApplicationCode.Should().Be("admin-portal");
    }

    [Fact]
    public void UserId_WithSubFallbackClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid().ToString();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId) }, "test-auth");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        service.UserId.Should().Be(Guid.Parse(userId));
    }

    [Fact]
    public void UserId_InvalidClaim_ReturnsNull()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "test-auth");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        service.UserId.Should().BeNull();
    }

    [Fact]
    public void MultipleRoles_ShouldReturnAll()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Manager"),
            new Claim(ClaimTypes.Role, "Viewer")
        }, "test-auth");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });

        service.Roles.Should().HaveCount(3);
        service.Roles.Should().Contain(new[] { "Admin", "Manager", "Viewer" });
    }
}
