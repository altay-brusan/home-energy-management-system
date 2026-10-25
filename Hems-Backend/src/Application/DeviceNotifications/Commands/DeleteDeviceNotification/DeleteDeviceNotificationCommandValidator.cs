using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.DeleteDeviceNotification;

public class DeleteDeviceNotificationCommandValidator : AbstractValidator<DeleteDeviceNotificationCommand>
{
    public DeleteDeviceNotificationCommandValidator()
    {
        RuleFor(v => v.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required.");
    }
}

