using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setBatteryChargeSourceConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class BatteryChargeSourceConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "BatteryChargeSourceConfigState";

    [JsonPropertyName("generator")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeneratorChargeConfig? Generator { get; set; }

    [JsonPropertyName("grid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GridChargeConfig? Grid { get; set; }
}

public class GeneratorChargeConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("startSocPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StartSocPercent { get; set; }

    [JsonPropertyName("chargeCurrentA")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChargeCurrentA { get; set; }

    [JsonPropertyName("maxRunTimeHours")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? MaxRunTimeHours { get; set; }

    [JsonPropertyName("coolingTimeHours")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? CoolingTimeHours { get; set; }
}

public class GridChargeConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("startSocPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StartSocPercent { get; set; }

    [JsonPropertyName("chargeCurrentA")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChargeCurrentA { get; set; }
}
