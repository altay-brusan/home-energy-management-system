namespace Hems_Backend.Application.Dashboard.Queries.GetBattery;

public record BatteryDto
{
    public List<BatteryDataPoint> DataPoints { get; init; } = new();
    public double AverageSoc { get; init; }
    public double PeakSoc { get; init; }
    public double CurrentSoc { get; init; }
}

public record BatteryDataPoint
{
    public DateTime DateTime { get; init; }
    public double Soc { get; init; } // State of Charge percentage
    public string Label { get; init; } = string.Empty; // "Hour-14:00", "14:30", etc.
}
