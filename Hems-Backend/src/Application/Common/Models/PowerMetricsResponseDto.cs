namespace Hems_Backend.Application.Common.Models;

public record PowerMetricsResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<PowerMetricsDataPoint> Data { get; init; } = new();
    public PowerMetricsStatistics Statistics { get; init; } = null!;
}

public record PowerMetricsDataPoint
{
    public string Timestamp { get; init; } = string.Empty;
    public double SolarPower { get; init; }
    public double GridPower { get; init; }
    public double BatteryPower { get; init; }
    public double LoadPower { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public record PowerMetricsStatistics
{
    public double AverageSolarPower { get; init; }
    public double PeakSolarPower { get; init; }
    public double AverageGridPower { get; init; }
    public double PeakGridPower { get; init; }
    public double AverageBatteryPower { get; init; }
    public double PeakBatteryPower { get; init; }
    public double AverageLoadPower { get; init; }
    public double PeakLoadPower { get; init; }
    public string Unit { get; init; } = string.Empty;
}
