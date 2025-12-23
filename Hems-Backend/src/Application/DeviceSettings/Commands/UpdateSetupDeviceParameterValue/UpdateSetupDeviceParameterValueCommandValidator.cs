using FluentValidation;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateSetupDeviceParameterValue;

public class UpdateSetupDeviceParameterValueCommandValidator : AbstractValidator<UpdateSetupDeviceParameterValueCommand>
{
    public UpdateSetupDeviceParameterValueCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID zorunludur.");

        RuleFor(v => v.Value)
            .NotEmpty().WithMessage("Değer zorunludur.");

        RuleFor(v => v.ModelNumber)
            .NotEmpty().WithMessage("Model numarası zorunludur.")
            .MaximumLength(100).WithMessage("Model numarası 100 karakterden fazla olamaz.");

        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıralama değeri 0'dan küçük olamaz.");
    }
}
