using FluentValidation;
using Hems_Backend.Application.DeviceSettings.Models;

namespace Hems_Backend.Application.DeviceSettings.Commands.SetDeviceMode;

public class SetDeviceModeValidator : AbstractValidator<SetDeviceModeCommand>
{
    public SetDeviceModeValidator()
    {
        RuleFor(x => x.SetupDeviceId)
            .NotEmpty()
            .WithMessage("SetupDeviceId is required");

        RuleFor(x => x.Mode)
            .IsInEnum()
            .WithMessage("Invalid device mode")
            .NotEqual(DeviceMode.Custom)
            .WithMessage("Custom mode cannot be applied directly. Use parameter update commands instead.");
    }
}
