namespace Hems_Backend.Application.Common.Models;

public record BatteryResponseDto
{
    public string Range { get; init; } = string.Empty;
    public List<BatteryDataPoint> Data { get; init; } = new();
    public BatteryStatistics Statistics { get; init; } = null!;
}

public record BatteryDataPoint
{
    public string Timestamp { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public record BatteryStatistics
{
    public double Average { get; init; }
    public double Peak { get; init; }
    public double Current { get; init; }
    public string Unit { get; init; } = string.Empty;
}
