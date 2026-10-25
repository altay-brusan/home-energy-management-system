namespace Hems_Backend.Application.Common.Models;

public record EnergyConsumptionResponseDto
{
    public string Range { get; init; } = string.Empty; // Day, Week, Month, Year
    public List<EnergyConsumptionDataPoint> Data { get; init; } = new();
    public EnergyConsumptionStatistics Statistics { get; init; } = null!;
}

public record EnergyConsumptionDataPoint
{
    public string Timestamp { get; init; } = string.Empty; // "00:00", "01:00", "12:00"
    public double Value { get; init; }
    public string Unit { get; init; } = "kWh";
}

public record EnergyConsumptionStatistics
{
    public double Total { get; init; }
    public double Peak { get; init; }
    public double Average { get; init; }
    public string Unit { get; init; } = "kWh";
}
