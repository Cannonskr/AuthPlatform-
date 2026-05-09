using Auth.Application.Features.Auth.Commands;
using Auth.Application.Features.Auth.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Auth.Application.Tests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyEmail_ShouldHaveError()
    {
        var command = new LoginCommand("", "Pass@123", "portal");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Fact]
    public void Validate_InvalidEmail_ShouldHaveError()
    {
        var command = new LoginCommand("not-an-email", "Pass@123", "portal");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void Validate_EmptyPassword_ShouldHaveError()
    {
        var command = new LoginCommand("admin@test.com", "", "portal");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Validate_EmptyApplicationCode_ShouldHaveError()
    {
        var command = new LoginCommand("admin@test.com", "Pass@123", "");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ApplicationCode)
            .WithErrorMessage("Application code is required.");
    }

    [Fact]
    public void Validate_ValidInput_ShouldNotHaveError()
    {
        var command = new LoginCommand("admin@test.com", "Pass@123", "admin-portal");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
