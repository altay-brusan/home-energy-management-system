namespace Hems_Backend.Application.Dashboard.Queries.GetInverterComponent;

public record InverterComponentDto
{
    public double Power { get; init; } // Anlık güç değeri
    public string PowerUnit { get; init; } = string.Empty; // "kW" veya "W"
    public double Today { get; init; } // Günlük toplam enerji (kWh)
    public double Total { get; init; } // Toplam lifetime enerji (kWh)
}
