using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(v => v.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required.");
    }
}
