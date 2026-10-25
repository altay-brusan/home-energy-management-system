using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.CreateDeviceNotification;

public class CreateDeviceNotificationCommandValidator : AbstractValidator<CreateDeviceNotificationCommand>
{
    public CreateDeviceNotificationCommandValidator()
    {
        RuleFor(v => v.DeviceId)
            .NotEmpty()
            .WithMessage("Device ID is required.");

        RuleFor(v => v.NotificationTypeCode)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Notification type code is required and must not exceed 50 characters.");

        RuleFor(v => v.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required.");

        RuleFor(v => v.AdditionalData)
            .MaximumLength(1000)
            .WithMessage("Additional data must not exceed 1000 characters.");
    }
}
