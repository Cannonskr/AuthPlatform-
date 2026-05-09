using Auth.Infrastructure.Services;
using FluentAssertions;

namespace Auth.Infrastructure.Tests.Services;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _service = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var hash = _service.Hash("Password@123");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2"); // BCrypt hash format
    }

    [Fact]
    public void Verify_CorrectPassword_ShouldReturnTrue()
    {
        var hash = _service.Hash("Password@123");

        var result = _service.Verify("Password@123", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ShouldReturnFalse()
    {
        var hash = _service.Hash("Password@123");

        var result = _service.Verify("WrongPassword", hash);

        result.Should().BeFalse();
    }
}
