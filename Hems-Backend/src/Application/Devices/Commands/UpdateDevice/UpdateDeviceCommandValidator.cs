using FluentValidation;

namespace Hems_Backend.Application.Devices.Commands.UpdateDevice;

public class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty()
            .WithMessage("Id is required.");

        RuleFor(v => v.Manufacturer)
            .MaximumLength(100)
            .NotEmpty()
            .WithMessage("Manufacturer is required and must not exceed 100 characters.");

        RuleFor(v => v.ModelNumber)
            .MaximumLength(100)
            .WithMessage("ModelNumber must not exceed 100 characters.");

        RuleFor(v => v.SerialNumber)
            .MaximumLength(100)
            .WithMessage("SerialNumber must not exceed 100 characters.");

        RuleFor(v => v.TypeNumber)
            .MaximumLength(100)
            .WithMessage("TypeNumber must not exceed 100 characters.");

        RuleFor(v => v.QRCode)
            .MaximumLength(100)
            .WithMessage("QRCode must not exceed 100 characters.");
    }
}







