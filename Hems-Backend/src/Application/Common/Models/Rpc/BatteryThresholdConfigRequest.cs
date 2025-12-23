using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setBatteryThresholdConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class BatteryThresholdConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "BatteryThresholdConfigState";

    [JsonPropertyName("shutdownCapacityPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ShutdownCapacityPercent { get; set; }

    [JsonPropertyName("restartCapacityPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RestartCapacityPercent { get; set; }

    [JsonPropertyName("lowBatteryCapacityPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LowBatteryCapacityPercent { get; set; }
}
