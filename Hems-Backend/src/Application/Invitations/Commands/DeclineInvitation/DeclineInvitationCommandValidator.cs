using FluentValidation;

namespace Hems_Backend.Application.Invitations.Commands.DeclineInvitation;

public class DeclineInvitationCommandValidator : AbstractValidator<DeclineInvitationCommand>
{
    public DeclineInvitationCommandValidator()
    {
        RuleFor(v => v.Token)
            .NotEmpty().WithMessage("Token zorunludur.")
            .Length(6).WithMessage("Token 6 haneli olmalıdır.")
            .Matches("^[0-9]+$").WithMessage("Token sadece rakamlardan oluşmalıdır.");
    }
}

