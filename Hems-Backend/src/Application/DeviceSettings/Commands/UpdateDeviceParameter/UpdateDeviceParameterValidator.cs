using FluentValidation;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameter;

public class UpdateDeviceParameterValidator : AbstractValidator<UpdateDeviceParameterCommand>
{
    public UpdateDeviceParameterValidator()
    {
        RuleFor(x => x.SetupDeviceId)
            .NotEmpty()
            .WithMessage("SetupDeviceId is required");

        RuleFor(x => x.ParameterId)
            .NotEmpty()
            .WithMessage("ParameterId is required");

        RuleFor(x => x.Value)
            .NotNull()
            .WithMessage("Value cannot be null");
    }
}
