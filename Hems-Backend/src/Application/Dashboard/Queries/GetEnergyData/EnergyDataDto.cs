namespace Hems_Backend.Application.Dashboard.Queries.GetEnergyData;

public record EnergyDataDto
{
    public DateTime Date { get; init; }
    
    // Enerji değerleri (kWh)
    public double PvEnergyKWh { get; init; }
    public double GridImportEnergyKWh { get; init; }
    public double GridExportEnergyKWh { get; init; }
    public double GeneratorEnergyKWh { get; init; }
    public double BatteryDischargeEnergyKWh { get; init; }
    public double BatteryChargeEnergyKWh { get; init; }
    public double LoadEnergyKWh { get; init; }
    
    // Toplam enerji alımı
    public double TotalEnergyInput { get; init; }
    
    // Yüzde değerleri - Girişler
    public double PvEnergyPercentage { get; init; }
    public double GridImportEnergyPercentage { get; init; }
    public double GeneratorEnergyPercentage { get; init; }
    public double BatteryChargeEnergyPercentage { get; init; }
    
    // Yüzde değerleri - Çıkışlar
    public double GridExportEnergyPercentage { get; init; }
    public double BatteryDischargeEnergyPercentage { get; init; }
    public double LoadEnergyPercentage { get; init; }
    
    // Toplam enerji çıkışı
    public double TotalEnergyOutput { get; init; }
}
