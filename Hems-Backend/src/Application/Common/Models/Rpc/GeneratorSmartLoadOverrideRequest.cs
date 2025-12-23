using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setGeneratorSmartLoadOverride RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class GeneratorSmartLoadOverrideRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "GeneratorSmartLoadOverride";

    [JsonPropertyName("forceSmartLoadOn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ForceSmartLoadOn { get; set; }
}
