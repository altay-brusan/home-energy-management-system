using FluentValidation;

namespace Hems_Backend.Application.MobileAppVersions.Commands.CreateMobileAppVersion;

public class CreateMobileAppVersionCommandValidator : AbstractValidator<CreateMobileAppVersionCommand>
{
    public CreateMobileAppVersionCommandValidator()
    {
        RuleFor(v => v.Platform)
            .IsInEnum().WithMessage("Platform must be a valid value (iOS or Android).");

        RuleFor(v => v.Version)
            .NotEmpty().WithMessage("Version is required.")
            .MaximumLength(50).WithMessage("Version must not exceed 50 characters.")
            .Matches(@"^\d+\.\d+\.\d+$").WithMessage("Version must be in format X.Y.Z (e.g., 1.0.0).");

        RuleFor(v => v.StoreUrl)
            .MaximumLength(500).WithMessage("Store URL must not exceed 500 characters.")
            .Must(BeAValidUrl).When(v => !string.IsNullOrEmpty(v.StoreUrl))
            .WithMessage("Store URL must be a valid URL.");

        RuleFor(v => v.ReleaseNotes)
            .MaximumLength(2000).WithMessage("Release notes must not exceed 2000 characters.");

        RuleFor(v => v.MinimumRequiredVersion)
            .MaximumLength(50).WithMessage("Minimum required version must not exceed 50 characters.")
            .Matches(@"^\d+\.\d+\.\d+$").When(v => !string.IsNullOrEmpty(v.MinimumRequiredVersion))
            .WithMessage("Minimum required version must be in format X.Y.Z (e.g., 1.0.0).");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
