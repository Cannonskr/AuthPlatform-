using Auth.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation.Results;

namespace Auth.Application.Tests.Common;

public class ExceptionTests
{
    [Fact]
    public void ValidationException_DefaultConstructor_SetsDefaultMessage()
    {
        var ex = new ValidationException();

        ex.Message.Should().Be("One or more validation failures have occurred.");
        ex.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationException_WithFailures_GroupsByPropertyName()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required."),
            new("Email", "Invalid email format."),
            new("Password", "Password is required.")
        };

        var ex = new ValidationException(failures);

        ex.Errors.Should().ContainKey("Email");
        ex.Errors["Email"].Should().HaveCount(2);
        ex.Errors["Email"].Should().Contain("Email is required.");
        ex.Errors["Email"].Should().Contain("Invalid email format.");
        ex.Errors.Should().ContainKey("Password");
        ex.Errors["Password"].Should().ContainSingle("Password is required.");
    }

    [Fact]
    public void NotFoundException_ShouldFormatMessage()
    {
        var ex = new NotFoundException("User", Guid.Parse("e5f6a7b8-c9d0-1234-ef12-345678901234"));

        ex.Message.Should().Be("Entity \"User\" (e5f6a7b8-c9d0-1234-ef12-345678901234) was not found.");
    }

    [Fact]
    public void UnauthorizedException_ShouldSetMessage()
    {
        var ex = new UnauthorizedException("Invalid token.");

        ex.Message.Should().Be("Invalid token.");
    }

    [Fact]
    public void ForbiddenException_ShouldSetMessage()
    {
        var ex = new ForbiddenException("Access denied.");

        ex.Message.Should().Be("Access denied.");
    }
}
