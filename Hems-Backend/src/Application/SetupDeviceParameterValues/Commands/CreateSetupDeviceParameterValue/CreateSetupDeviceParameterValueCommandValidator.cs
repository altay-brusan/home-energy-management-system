using FluentValidation;

namespace Hems_Backend.Application.SetupDeviceParameterValues.Commands.CreateSetupDeviceParameterValue;

public class CreateSetupDeviceParameterValueCommandValidator : AbstractValidator<CreateSetupDeviceParameterValueCommand>
{
    public CreateSetupDeviceParameterValueCommandValidator()
    {
        RuleFor(v => v.ParameterId)
            .NotEmpty().WithMessage("Parametre ID'si zorunludur.");

        RuleFor(v => v.SetupDeviceId)
            .NotEmpty().WithMessage("Setup device ID'si zorunludur.");

        RuleFor(v => v.Value)
            .NotEmpty().WithMessage("Değer zorunludur.")
            .MaximumLength(1000).WithMessage("Değer 1000 karakterden fazla olamaz.");


        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıralama değeri 0'dan küçük olamaz.");
    }
}
