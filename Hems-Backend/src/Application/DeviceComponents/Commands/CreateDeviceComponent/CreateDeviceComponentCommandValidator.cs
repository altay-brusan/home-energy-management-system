using FluentValidation;

namespace Hems_Backend.Application.DeviceComponents.Commands.CreateDeviceComponent;

public class CreateDeviceComponentCommandValidator : AbstractValidator<CreateDeviceComponentCommand>
{
    public CreateDeviceComponentCommandValidator()
    {
        RuleFor(v => v.DeviceId)
            .NotEmpty();

        RuleFor(v => v.Name)
            .MaximumLength(200)
            .NotEmpty();

        RuleFor(v => v.Manufacturer)
            .MaximumLength(100);

        RuleFor(v => v.ModelNumber)
            .MaximumLength(100);

        RuleFor(v => v.SerialNumber)
            .MaximumLength(100);
    }
}

