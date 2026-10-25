using FluentValidation;

namespace Hems_Backend.Application.Invitations.Commands.RemoveUserFromSite;

public class RemoveUserFromSiteCommandValidator : AbstractValidator<RemoveUserFromSiteCommand>
{
    public RemoveUserFromSiteCommandValidator()
    {
        RuleFor(v => v.InvitationId)
            .NotEmpty().WithMessage("Invitation ID zorunludur.");
    }
}

