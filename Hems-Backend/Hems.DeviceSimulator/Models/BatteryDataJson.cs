using System.Text.Json.Serialization;

namespace Hems.DeviceSimulator.Models;

public class BatteryDataJson
{
    [JsonPropertyName("Battery Pack Voltage")]
    public double BatteryPackVoltage { get; set; }
    
    [JsonPropertyName("Battery Current")]
    public double BatteryCurrent { get; set; }
    
    [JsonPropertyName("SOH")]
    public List<double> Soh { get; set; } = new();
    
    [JsonPropertyName("SOC Cumulative")]
    public List<double> SocCumulative { get; set; } = new();
    
    [JsonPropertyName("Soc")]
    public List<BatteryCellJson> Soc { get; set; } = new();
    
    [JsonPropertyName("Total Charge Capacity")]
    public string TotalChargeCapacity { get; set; } = string.Empty;
    
    [JsonPropertyName("Total Discharge Amount")]
    public double TotalDischargeAmount { get; set; }
    
    [JsonPropertyName("Max Cell Voltage")]
    public double MaxCellVoltage { get; set; }
    
    [JsonPropertyName("Min Cell Voltage")]
    public double MinCellVoltage { get; set; }
    
    [JsonPropertyName("Max Cell Temperature")]
    public double MaxCellTemperature { get; set; }
    
    [JsonPropertyName("Min Cell Temperature")]
    public double MinCellTemperature { get; set; }
    
    [JsonPropertyName("Device Model")]
    public string DeviceModel { get; set; } = string.Empty;
    
    [JsonPropertyName("Device Serial Number")]
    public string DeviceSerialNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("Firmware Version")]
    public string FirmwareVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("DateTime")]
    public string DateTime { get; set; } = string.Empty;
}

public class BatteryCellJson
{
    [JsonPropertyName("Cell Number")]
    public int CellNumber { get; set; }
    
    [JsonPropertyName("Cell Voltage")]
    public double CellVoltage { get; set; }
    
    [JsonPropertyName("Cell Temperature")]
    public double CellTemperature { get; set; }
}
