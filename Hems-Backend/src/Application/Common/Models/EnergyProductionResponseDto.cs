namespace Hems_Backend.Application.Common.Models;

public record EnergyProductionResponseDto
{
    public string Range { get; init; } = string.Empty; // Day, Week, Month, Year
    public List<EnergyProductionDataPoint> Data { get; init; } = new();
    public EnergyProductionStatistics Statistics { get; init; } = null!;
}

public record EnergyProductionDataPoint
{
    public string Timestamp { get; init; } = string.Empty; // "00:00", "01:00", "12:00"
    public double Value { get; init; }
    public string Unit { get; init; } = "kWh";
}

public record EnergyProductionStatistics
{
    public double Total { get; init; }
    public double Peak { get; init; }
    public double Average { get; init; }
    public string Unit { get; init; } = "kWh";
}
