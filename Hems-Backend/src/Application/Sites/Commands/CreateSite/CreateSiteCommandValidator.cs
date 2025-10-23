using FluentValidation;

namespace Hems_Backend.Application.Sites.Commands.CreateSite;

public class CreateSiteCommandValidator : AbstractValidator<CreateSiteCommand>
{
    public CreateSiteCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");

        RuleFor(v => v.Country)
            .MaximumLength(100)
            .WithMessage("Country must not exceed 100 characters.");

        RuleFor(v => v.City)
            .MaximumLength(100)
            .WithMessage("City must not exceed 100 characters.");

        RuleFor(v => v.District)
            .MaximumLength(100)
            .WithMessage("District must not exceed 100 characters.");

        RuleFor(v => v.AddressLine)
            .MaximumLength(500)
            .WithMessage("AddressLine must not exceed 500 characters.");

        RuleFor(v => v.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(v => v.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(v => v.Timezone)
            .NotEmpty()
            .WithMessage("Timezone is required.")
            .MaximumLength(50)
            .WithMessage("Timezone must not exceed 50 characters.");
    }
}

