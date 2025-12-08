using FluentValidation;
using Hems_Backend.Application.Common.Enums;

namespace Hems_Backend.Application.DeviceSettings.Commands.ChangeExportStrategy;

public class ChangeExportStrategyValidator : AbstractValidator<ChangeExportStrategyCommand>
{
    public ChangeExportStrategyValidator()
    {
        RuleFor(v => v.DeviceId)
            .NotEmpty()
            .WithMessage("DeviceId is required.");

        RuleFor(v => v.ExportStrategy)
            .IsInEnum()
            .WithMessage("ExportStrategy must be a valid value (SellFirst, ZeroExportToLoad, or ZeroExportToCt).");
    }
}
