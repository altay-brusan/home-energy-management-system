using FluentValidation;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;

public class CreateDeviceEnergyDataCommandValidator : AbstractValidator<CreateDeviceEnergyDataCommand>
{
    public CreateDeviceEnergyDataCommandValidator()
    {
        RuleFor(v => v.DataType)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(v => v.DeviceId)
            .NotEmpty();

        RuleFor(v => v.DataDateTime)
            .NotEmpty();

        // Günlük değerler için validasyon
        RuleFor(v => v.PvEnergyKWhToday)
            .GreaterThanOrEqualTo(0)
            .When(v => v.PvEnergyKWhToday.HasValue);

        RuleFor(v => v.GridImportEnergyKWhToday)
            .GreaterThanOrEqualTo(0)
            .When(v => v.GridImportEnergyKWhToday.HasValue);

        RuleFor(v => v.GridExportEnergyKWhToday)
            .GreaterThanOrEqualTo(0)
            .When(v => v.GridExportEnergyKWhToday.HasValue);

        RuleFor(v => v.GeneratorEnergyKWhToday)
            .GreaterThanOrEqualTo(0)
            .When(v => v.GeneratorEnergyKWhToday.HasValue);

        RuleFor(v => v.BatteryDischargeEnergyKWhToday)
            .GreaterThanOrEqualTo(0)
            .When(v => v.BatteryDischargeEnergyKWhToday.HasValue);

        RuleFor(v => v.BatteryChargeEnergyKWhToday)
            .GreaterThanOrEqualTo(0)
            .When(v => v.BatteryChargeEnergyKWhToday.HasValue);

        RuleFor(v => v.LoadEnergyKWhToday)
            .GreaterThanOrEqualTo(0)
            .When(v => v.LoadEnergyKWhToday.HasValue);

        // Ömür boyu değerler için validasyon
        RuleFor(v => v.PvEnergyKWhTotal)
            .GreaterThanOrEqualTo(0)
            .When(v => v.PvEnergyKWhTotal.HasValue);

        RuleFor(v => v.GridImportEnergyKWhTotal)
            .GreaterThanOrEqualTo(0)
            .When(v => v.GridImportEnergyKWhTotal.HasValue);

        RuleFor(v => v.GridExportEnergyKWhTotal)
            .GreaterThanOrEqualTo(0)
            .When(v => v.GridExportEnergyKWhTotal.HasValue);

        RuleFor(v => v.GeneratorEnergyKWhTotal)
            .GreaterThanOrEqualTo(0)
            .When(v => v.GeneratorEnergyKWhTotal.HasValue);

        RuleFor(v => v.BatteryDischargeEnergyKWhTotal)
            .GreaterThanOrEqualTo(0)
            .When(v => v.BatteryDischargeEnergyKWhTotal.HasValue);

        RuleFor(v => v.BatteryChargeEnergyKWhTotal)
            .GreaterThanOrEqualTo(0)
            .When(v => v.BatteryChargeEnergyKWhTotal.HasValue);

        RuleFor(v => v.LoadEnergyKWhTotal)
            .GreaterThanOrEqualTo(0)
            .When(v => v.LoadEnergyKWhTotal.HasValue);
    }
}
