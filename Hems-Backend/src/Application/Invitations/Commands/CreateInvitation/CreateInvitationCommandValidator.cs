using FluentValidation;

namespace Hems_Backend.Application.Invitations.Commands.CreateInvitation;

public class CreateInvitationCommandValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");

        RuleFor(v => v.SiteId)
            .NotEmpty().WithMessage("Site ID is required.");
    }
}

