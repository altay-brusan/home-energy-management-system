using FluentValidation;

namespace Hems_Backend.Application.Invitations.Commands.AcceptInvitation;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(v => v.Token)
            .NotEmpty().WithMessage("Token zorunludur.")
            .Length(6).WithMessage("Token 6 haneli olmalıdır.")
            .Matches("^[0-9]+$").WithMessage("Token sadece rakamlardan oluşmalıdır.");
    }
}

