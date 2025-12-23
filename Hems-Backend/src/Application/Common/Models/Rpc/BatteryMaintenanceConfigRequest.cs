using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setBatteryMaintenanceConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class BatteryMaintenanceConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "BatteryMaintenanceConfigState";

    [JsonPropertyName("batteryControlMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BatteryControlMode { get; set; }

    [JsonPropertyName("wakeUp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BatteryWakeUpConfig? WakeUp { get; set; }

    [JsonPropertyName("batteryResistanceMilliOhm")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BatteryResistanceMilliOhm { get; set; }

    [JsonPropertyName("chargeEfficiencyPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ChargeEfficiencyPercent { get; set; }
}

public class BatteryWakeUpConfig
{
    [JsonPropertyName("channel1Enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Channel1Enabled { get; set; }
}
