using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setInverterAdvancedFunctionConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class InverterAdvancedFunctionConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "InverterAdvancedFunctionConfigState";

    [JsonPropertyName("arcFault")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArcFaultConfig? ArcFault { get; set; }

    [JsonPropertyName("islandProtectionEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IslandProtectionEnabled { get; set; }

    [JsonPropertyName("parallel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParallelConfig? Parallel { get; set; }

    [JsonPropertyName("externalMeter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExternalMeterConfig? ExternalMeter { get; set; }

    [JsonPropertyName("lithiumBatteryLossFaultEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LithiumBatteryLossFaultEnabled { get; set; }
}

public class ArcFaultConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }
}

public class ParallelConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; set; }

    [JsonPropertyName("phase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phase { get; set; }

    [JsonPropertyName("modbusAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ModbusAddress { get; set; }
}

public class ExternalMeterConfig
{
    [JsonPropertyName("ctRatio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CtRatio { get; set; }

    [JsonPropertyName("meterCtRatio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MeterCtRatio { get; set; }
}
