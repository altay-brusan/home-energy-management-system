using FluentValidation;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateDeviceParameters;

public class UpdateDeviceParametersValidator : AbstractValidator<UpdateDeviceParametersCommand>
{
    public UpdateDeviceParametersValidator()
    {
        RuleFor(x => x.SetupDeviceId)
            .NotEmpty()
            .WithMessage("SetupDeviceId is required");

        RuleFor(x => x.Parameters)
            .NotEmpty()
            .WithMessage("At least one parameter update is required");

        RuleForEach(x => x.Parameters)
            .ChildRules(parameter =>
            {
                parameter.RuleFor(p => p.ParameterId)
                    .NotEmpty()
                    .WithMessage("ParameterId is required");

                parameter.RuleFor(p => p.Value)
                    .NotNull()
                    .WithMessage("Value cannot be null");
            });
    }
}
