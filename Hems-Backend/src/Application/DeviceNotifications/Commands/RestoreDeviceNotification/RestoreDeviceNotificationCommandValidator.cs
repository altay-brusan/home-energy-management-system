using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.RestoreDeviceNotification;

public class RestoreDeviceNotificationCommandValidator : AbstractValidator<RestoreDeviceNotificationCommand>
{
    public RestoreDeviceNotificationCommandValidator()
    {
        RuleFor(v => v.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required.");
    }
}

