using FluentValidation;

namespace Hems_Backend.Application.Setups.Commands.UpdateSetup;

public class UpdateSetupCommandValidator : AbstractValidator<UpdateSetupCommand>
{
    public UpdateSetupCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty()
            .WithMessage("Id is required.");

        RuleFor(v => v.SiteId)
            .NotEmpty()
            .WithMessage("SiteId is required.");

        RuleFor(v => v.Name)
            .MaximumLength(200)
            .NotEmpty()
            .WithMessage("Name is required and must not exceed 200 characters.");
    }
}



















