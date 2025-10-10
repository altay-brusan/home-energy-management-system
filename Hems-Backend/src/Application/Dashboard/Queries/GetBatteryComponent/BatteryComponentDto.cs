namespace Hems_Backend.Application.Dashboard.Queries.GetBatteryComponent;

public record BatteryComponentDto
{
    public double SOC { get; init; } // State of Charge percentage
    public double Today { get; init; } // Günlük battery charge (kWh)
    public double Total { get; init; } // Toplam battery charge (kWh)
}
