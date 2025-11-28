namespace Hems_Backend.Application.Common.Models;

public record PowerMetricsResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<PowerMetricsDataPoint> Data { get; init; } = new();   
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

