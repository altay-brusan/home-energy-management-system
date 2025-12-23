using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setGeneratorAndSmartLoadConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class GeneratorAndSmartLoadConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "GeneratorAndSmartLoadConfigState";

    [JsonPropertyName("generatorChargeEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GeneratorChargeEnabled { get; set; }

    [JsonPropertyName("gridChargeEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GridChargeEnabled { get; set; }

    [JsonPropertyName("startSocPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StartSocPercent { get; set; }

    [JsonPropertyName("chargeCurrentA")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChargeCurrentA { get; set; }

    [JsonPropertyName("generatorPeakShaving")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeneratorPeakShavingConfig? GeneratorPeakShaving { get; set; }

    [JsonPropertyName("smartLoad")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SmartLoadConfig? SmartLoad { get; set; }
}

public class GeneratorPeakShavingConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("limitPowerW")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LimitPowerW { get; set; }
}

public class SmartLoadConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Enabled { get; set; }

    [JsonPropertyName("offSocPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OffSocPercent { get; set; }

    [JsonPropertyName("onSocPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OnSocPercent { get; set; }

    [JsonPropertyName("pvPowerThresholdW")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PvPowerThresholdW { get; set; }

    [JsonPropertyName("openDelayMinutes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OpenDelayMinutes { get; set; }

    [JsonPropertyName("onGridAlwaysOn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OnGridAlwaysOn { get; set; }
}
