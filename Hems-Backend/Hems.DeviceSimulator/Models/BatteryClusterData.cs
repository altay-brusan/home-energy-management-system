using System.Text.Json.Serialization;

namespace Hems.DeviceSimulator.Models;

public class BatteryClusterData
{
    [JsonPropertyName("Voltage Pack")]
    public double VoltagePack { get; set; }
    
    [JsonPropertyName("Current Pack")]
    public double CurrentPack { get; set; }
    
    [JsonPropertyName("Max Voltage Of Battery Cluster")]
    public double MaxVoltageOfBatteryCluster { get; set; }
    
    [JsonPropertyName("Min Voltage Of Battery Cluster")]
    public double MinVoltageOfBatteryCluster { get; set; }
    
    [JsonPropertyName("Soc")]
    public double Soc { get; set; }
    
    [JsonPropertyName("Total Charge Capacity")]
    public double TotalChargeCapacity { get; set; }
    
    [JsonPropertyName("Total Discharge Capacity")]
    public double TotalDischargeCapacity { get; set; }
    
    [JsonPropertyName("DateTime")]
    public string DateTime { get; set; } = string.Empty;
    
 
}
