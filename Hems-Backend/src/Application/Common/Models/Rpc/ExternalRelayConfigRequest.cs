using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setExternalRelayConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class ExternalRelayConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "ExternalRelayConfigState";

    [JsonPropertyName("externalRelayFeatureEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ExternalRelayFeatureEnabled { get; set; }

    [JsonPropertyName("relayBitmapRaw")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RelayBitmapRaw { get; set; }
}
