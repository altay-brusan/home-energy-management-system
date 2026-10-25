using System.Text.Json.Serialization;

namespace Hems.DeviceSimulator.Models;

public class EnergyFlowData
{
    [JsonPropertyName("dateTime")]
    public DateTime DateTime { get; set; }
    
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;
    
    // Today values (for hourly data)
    [JsonPropertyName("pvEnergyKWhToday")]
    public double PvEnergyKWhToday { get; set; }
    
    [JsonPropertyName("gridImportEnergyKWhToday")]
    public double GridImportEnergyKWhToday { get; set; }
    
    [JsonPropertyName("gridExportEnergyKWhToday")]
    public double GridExportEnergyKWhToday { get; set; }
    
    [JsonPropertyName("generatorEnergyKWhToday")]
    public double GeneratorEnergyKWhToday { get; set; }
    
    [JsonPropertyName("batteryDischargeEnergyKWhToday")]
    public double BatteryDischargeEnergyKWhToday { get; set; }
    
    [JsonPropertyName("batteryChargeEnergyKWhToday")]
    public double BatteryChargeEnergyKWhToday { get; set; }
    
    [JsonPropertyName("loadEnergyKWhToday")]
    public double LoadEnergyKWhToday { get; set; }
    
    // Lifetime values (for midnight data)
    [JsonPropertyName("pvEnergyKWhTotal")]
    public double PvEnergyKWhTotal { get; set; }
    
    [JsonPropertyName("gridImportEnergyKWhTotal")]
    public double GridImportEnergyKWhTotal { get; set; }
    
    [JsonPropertyName("gridExportEnergyKWhTotal")]
    public double GridExportEnergyKWhTotal { get; set; }
    
    [JsonPropertyName("generatorEnergyKWhTotal")]
    public double GeneratorEnergyKWhTotal { get; set; }
    
    [JsonPropertyName("batteryDischargeEnergyKWhTotal")]
    public double BatteryDischargeEnergyKWhTotal { get; set; }
    
    [JsonPropertyName("batteryChargeEnergyKWhTotal")]
    public double BatteryChargeEnergyKWhTotal { get; set; }
    
    [JsonPropertyName("loadEnergyKWhTotal")]
    public double LoadEnergyKWhTotal { get; set; }
}
