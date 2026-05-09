using Auth.Application.Common.Models;
using FluentAssertions;

namespace Auth.Application.Tests.Common;

public class ModelTests
{
    [Fact]
    public void AuthenticationResult_Succeed_Factory()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var user = new UserDto { Id = Guid.NewGuid(), Email = "test@test.com", Username = "test" };

        var result = AuthenticationResult.Succeed("jwt-token", "refresh-token", expiresAt, user);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("jwt-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresAt.Should().Be(expiresAt);
        result.User.Should().Be(user);
        result.Errors.Should().BeNull();
    }

    [Fact]
    public void AuthenticationResult_Failed_Factory()
    {
        var result = AuthenticationResult.Failed("Error 1", "Error 2");

        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
        result.AccessToken.Should().BeNull();
        result.User.Should().BeNull();
    }

    [Fact]
    public void PagedResult_ComputedProperties()
    {
        var result = new PagedResult<string>
        {
            Items = new[] { "a", "b", "c" },
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_LastPage_HasNoNextPage()
    {
        var result = new PagedResult<string>
        {
            Items = new[] { "a", "b" },
            TotalCount = 12,
            Page = 2,
            PageSize = 10
        };

        result.TotalPages.Should().Be(2);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void UserDto_DefaultValues()
    {
        var dto = new UserDto();

        dto.Roles.Should().BeEmpty();
        dto.Permissions.Should().BeEmpty();
    }
}
