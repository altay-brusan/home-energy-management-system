using FluentValidation;

namespace Hems_Backend.Application.DeviceComponents.Commands.UpdateDeviceComponent;

public class UpdateDeviceComponentCommandValidator : AbstractValidator<UpdateDeviceComponentCommand>
{
    public UpdateDeviceComponentCommandValidator()
    {
        RuleFor(v => v.Id)
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

