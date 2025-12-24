using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;

public class ProcessDeviceAlarmCommandValidator : AbstractValidator<ProcessDeviceAlarmCommand>
{
    public ProcessDeviceAlarmCommandValidator()
    {
        RuleFor(v => v.DeviceId)
            .NotEmpty()
            .WithMessage("DeviceId is required.");

        RuleFor(v => v.AlarmState)
            .NotNull()
            .WithMessage("AlarmState is required.");

        RuleFor(v => v.AlarmState.TimestampUtc)
            .NotEmpty()
            .WithMessage("TimestampUtc is required.")
            .When(v => v.AlarmState != null);

        RuleFor(v => v.AlarmState.GatewayId)
            .NotEmpty()
            .WithMessage("GatewayId is required.")
            .When(v => v.AlarmState != null);

        RuleFor(v => v.AlarmState.ActiveAlarms)
            .NotNull()
            .WithMessage("ActiveAlarms array is required.")
            .When(v => v.AlarmState != null);
    }
}
