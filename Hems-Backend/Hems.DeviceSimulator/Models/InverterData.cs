using System.Text.Json.Serialization;

namespace Hems.DeviceSimulator.Models;

public class InverterData
{
    [JsonPropertyName("Last Inverter Startup Time")]
    public string LastInverterStartupTime { get; set; } = string.Empty;
    
    [JsonPropertyName("Last Inverter Shutdown Time")]
    public string LastInverterShutdownTime { get; set; } = string.Empty;
    
    [JsonPropertyName("Active Power")]
    public double ActivePower { get; set; }
    
    [JsonPropertyName("Reactive Power")]
    public double ReactivePower { get; set; }
    
    [JsonPropertyName("Power Factor")]
    public double PowerFactor { get; set; }
    
    [JsonPropertyName("Phases")]
    public List<InverterPhase> Phases { get; set; } = new();
    
    [JsonPropertyName("Insulation Resistance")]
    public double InsulationResistance { get; set; }
    
    [JsonPropertyName("Device Serial Number")]
    public string DeviceSerialNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("Firmware Version")]
    public string FirmwareVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("DateTime")]
    public string DateTime { get; set; } = string.Empty;
}

public class InverterPhase
{
    [JsonPropertyName("Phase Id")]
    public string PhaseId { get; set; } = string.Empty;
    
    [JsonPropertyName("Voltage")]
    public double Voltage { get; set; }
    
    [JsonPropertyName("Current")]
    public double Current { get; set; }
}