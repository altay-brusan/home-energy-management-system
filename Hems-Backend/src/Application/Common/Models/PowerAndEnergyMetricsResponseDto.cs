namespace Hems_Backend.Application.Common.Models;

public record PowerAndEnergyMetricsResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<PowerAndEnergyMetricsDataPoint> Data { get; init; } = new();   
}

public record PowerAndEnergyMetricsDataPoint
{
    public string Timestamp { get; init; } = string.Empty;
    public double SolarPower { get; init; }
    public double GridPower { get; init; }
    public double BatteryPower { get; init; }
    public double LoadPower { get; init; }
    public double GeneratorPower { get; init; }

    public double? GridImportEnergyKWh { get; init; }
    public double? GridExportEnergyKWh { get; init; }
    public double? GeneratorEnergyKWh { get; init; }
    public double? BatteryDischargeEnergyKWh { get; init; }
    public double? BatteryChargeEnergyKWh { get; init; }
    public double? LoadEnergyKWh { get; init; }

    public string Unit { get; init; } = string.Empty;
}

