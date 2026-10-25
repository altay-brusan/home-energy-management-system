using FluentValidation;

namespace Hems_Backend.Application.Invitations.Commands.DeclineInvitation;

public class DeclineInvitationCommandValidator : AbstractValidator<DeclineInvitationCommand>
{
    public DeclineInvitationCommandValidator()
    {
        RuleFor(v => v.Token)
            .NotEmpty().WithMessage("Token is required.")
            .Length(6).WithMessage("Token must be 6 digits.")
            .Matches("^[0-9]+$").WithMessage("Token must contain only numbers.");
    }
}

