using FluentValidation;

namespace Hems_Backend.Application.Setups.Commands.CreateSetup;

public class CreateSetupCommandValidator : AbstractValidator<CreateSetupCommand>
{
    public CreateSetupCommandValidator()
    {
        RuleFor(v => v.SiteId)
            .NotEmpty()
            .WithMessage("SiteId is required.");

        RuleFor(v => v.Name)
            .MaximumLength(200)
            .NotEmpty()
            .WithMessage("Name is required and must not exceed 200 characters.");

        // QR Code veya Serial Number'dan en az biri verilmeli
        RuleFor(v => v)
            .Must(x => !string.IsNullOrEmpty(x.DeviceQRCode) || !string.IsNullOrEmpty(x.DeviceSerialNumber))
            .WithMessage("Either DeviceQRCode or DeviceSerialNumber must be provided.");

        RuleFor(v => v.DeviceQRCode)
            .MaximumLength(100)
            .When(v => !string.IsNullOrEmpty(v.DeviceQRCode));

        RuleFor(v => v.DeviceSerialNumber)
            .MaximumLength(100)
            .When(v => !string.IsNullOrEmpty(v.DeviceSerialNumber));
    }
}








