using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setGridCodeConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class GridCodeConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "GridCodeConfigState";

    [JsonPropertyName("gridModeRaw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GridModeRaw { get; set; }

    [JsonPropertyName("gridFrequencyHz")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GridFrequencyHz { get; set; }

    [JsonPropertyName("outputVoltageLevelCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OutputVoltageLevelCode { get; set; }

    [JsonPropertyName("normalConnect")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NormalConnectConfig? NormalConnect { get; set; }

    [JsonPropertyName("reconnectionTimeSeconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ReconnectionTimeSeconds { get; set; }

    [JsonPropertyName("powerFactorTarget")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PowerFactorTarget { get; set; }

    [JsonPropertyName("activePowerRegulationPercentPerStep")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ActivePowerRegulationPercentPerStep { get; set; }

    [JsonPropertyName("reactivePowerRegulationPercentPerStep")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ReactivePowerRegulationPercentPerStep { get; set; }
}

public class NormalConnectConfig
{
    [JsonPropertyName("voltageHighV")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? VoltageHighV { get; set; }

    [JsonPropertyName("voltageLowV")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? VoltageLowV { get; set; }

    [JsonPropertyName("frequencyHighHz")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? FrequencyHighHz { get; set; }

    [JsonPropertyName("frequencyLowHz")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? FrequencyLowHz { get; set; }
}
