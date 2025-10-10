namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public record PowerMetricsDto
{
    public string Period { get; init; } = string.Empty; // daily, weekly, monthly, yearly
    public List<PowerMetricsDataPoint> DataPoints { get; init; } = new();
}

public record PowerMetricsDataPoint
{
    public DateTime DateTime { get; init; }
    
    // Power değerleri (kW) - Net hesaplamalar
    public double SolarPowerKw { get; init; } // PvEnergyKWh (pozitif)
    public double GridPowerKw { get; init; } // GridImport - GridExport (pozitif/negatif)
    public double BatteryPowerKw { get; init; } // BatteryCharge - BatteryDischarge (pozitif/negatif)
    public double LoadPowerKw { get; init; } // -LoadEnergyKWh (negatif)
    
    public string Label { get; init; } = string.Empty; // "Hour-14:00", "Day-Monday", etc.
}
