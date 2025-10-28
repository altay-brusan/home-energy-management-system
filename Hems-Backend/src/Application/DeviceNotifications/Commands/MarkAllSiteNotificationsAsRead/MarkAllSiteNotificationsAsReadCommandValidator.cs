using FluentValidation;

namespace Hems_Backend.Application.DeviceNotifications.Commands.MarkAllSiteNotificationsAsRead;

public class MarkAllSiteNotificationsAsReadCommandValidator : AbstractValidator<MarkAllSiteNotificationsAsReadCommand>
{
    public MarkAllSiteNotificationsAsReadCommandValidator()
    {
        RuleFor(v => v.SiteId)
            .NotEmpty()
            .WithMessage("Site ID is required.");
    }
}
