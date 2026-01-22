using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setBatteryCapacityConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class BatteryCapacityConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "BatteryCapacityConfigState";

    [JsonPropertyName("batteryCapacityAh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BatteryCapacityAh { get; set; }

    [JsonPropertyName("maxChargeCurrentA")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxChargeCurrentA { get; set; }

    [JsonPropertyName("maxDischargeCurrentA")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxDischargeCurrentA { get; set; }
}
