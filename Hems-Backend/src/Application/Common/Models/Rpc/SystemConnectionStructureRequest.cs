using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setSystemConnectionStructure RPC
/// All properties are nullable - only set properties will be serialized
/// Based on the RPC definition, fields are mostly at root level (flat structure)
/// </summary>
public class SystemConnectionStructureRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "SystemConnectionStructure";

    // Root-level fields (flat structure as per RPC spec)
    [JsonPropertyName("gridType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GridType { get; set; }

    [JsonPropertyName("gridStandardRaw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GridStandardRaw { get; set; }

    [JsonPropertyName("gridFrequencyHz")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GridFrequencyHz { get; set; }

    [JsonPropertyName("gridVoltageLevelCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GridVoltageLevelCode { get; set; }

    [JsonPropertyName("gridPhaseSequence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? GridPhaseSequence { get; set; }  // Can be int (0/1) or string ("ABC_0_120_240"/"ACB_0_240_120")

    [JsonPropertyName("batteryControlMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? BatteryControlMode { get; set; }  // Can be int (0-2) or string

    [JsonPropertyName("exportLimitMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ExportLimitMode { get; set; }  // Can be int (0-2) or string

    [JsonPropertyName("itSystemNeutralUngrounded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ItSystemNeutralUngrounded { get; set; }

    // Nested objects
    [JsonPropertyName("generator")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SystemConnectionGeneratorConfig? Generator { get; set; }

    // For backwards compatibility with field path "acSide.xxx"
    [JsonPropertyName("acSide")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SystemConnectionAcSideConfig? AcSide { get; set; }

    // For backwards compatibility with field path "inverter.acSide.xxx"
    [JsonPropertyName("inverter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SystemConnectionInverterConfig? Inverter { get; set; }
}

public class SystemConnectionGeneratorConfig
{
    [JsonPropertyName("inputMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? InputMode { get; set; }  // Can be int (0-2) or string

    [JsonPropertyName("connectedToGridInput")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ConnectedToGridInput { get; set; }
}

public class SystemConnectionInverterConfig
{
    [JsonPropertyName("acSide")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SystemConnectionAcSideConfig? AcSide { get; set; }
}

public class SystemConnectionAcSideConfig
{
    [JsonPropertyName("itSystemNeutralUngrounded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ItSystemNeutralUngrounded { get; set; }

    [JsonPropertyName("gridPhaseSequence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? GridPhaseSequence { get; set; }

    [JsonPropertyName("gridVoltageLevelCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GridVoltageLevelCode { get; set; }
}
