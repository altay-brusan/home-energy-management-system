using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.PurgeDeviceNotification;

public class PurgeDeviceNotificationCommandValidator : AbstractValidator<PurgeDeviceNotificationCommand>
{
    public PurgeDeviceNotificationCommandValidator()
    {
        RuleFor(v => v.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required.");
    }
}
