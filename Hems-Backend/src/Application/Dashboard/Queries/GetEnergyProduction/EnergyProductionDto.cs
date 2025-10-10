namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyProduction;

public record EnergyProductionDto
{
    public string Period { get; init; } = string.Empty; // daily, weekly, monthly, yearly
    public List<EnergyProductionDataPoint> DataPoints { get; init; } = new();
}

public record EnergyProductionDataPoint
{
    public DateTime DateTime { get; init; }
    public double TotalProductionKWh { get; init; } // PvEnergyKWh + GeneratorEnergyKWh
    public double PvEnergyKWh { get; init; }
    public double GeneratorEnergyKWh { get; init; }
    public string Label { get; init; } = string.Empty; // "14:00", "Pazartesi", "Ocak", etc.
}
