using Auth.Application.Features.Auth.Commands;
using FluentValidation;

namespace Auth.Application.Features.Auth.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.");

        RuleFor(v => v.ApplicationCode)
            .NotEmpty().WithMessage("Application code is required.");
    }
}
