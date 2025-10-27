using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.PermanentDeleteDeviceNotification;

public class PermanentDeleteDeviceNotificationCommandValidator : AbstractValidator<PermanentDeleteDeviceNotificationCommand>
{
    public PermanentDeleteDeviceNotificationCommandValidator()
    {
        RuleFor(v => v.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required.");
    }
}








