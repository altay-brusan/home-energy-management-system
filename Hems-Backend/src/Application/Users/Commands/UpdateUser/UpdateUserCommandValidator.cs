using FluentValidation;

namespace Hems_Backend.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(v => v.FirstName)
            .MaximumLength(50)
            .WithMessage("First name must not exceed 50 characters.");

        RuleFor(v => v.Surname)
            .MaximumLength(50)
            .WithMessage("Surname must not exceed 50 characters.");

        RuleFor(v => v.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.");

        RuleFor(v => v.PhoneNumber)
            .Matches(@"^[\+]?[1-9][\d]{0,15}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("A valid phone number is required.");
    }
}
