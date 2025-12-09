using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// RPC response for getInverterIdentity
/// </summary>
public class InverterIdentityDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("modelNumber")]
    public string? ModelNumber { get; set; }

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Additional properties that will be stored in Specifications
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>
/// RPC response for getInverterState
/// </summary>
public class InverterDeviceStateDto
{
    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("runState")]
    public string? RunState { get; set; } // "standby" | "selfcheck" | "normal" | "alarm" | "fault" | "activating"

    // --- Output to AC bus ---
    [JsonPropertyName("outputActivePowerW")]
    public int? OutputActivePowerW { get; set; }

    [JsonPropertyName("outputApparentPowerVA")]
    public int? OutputApparentPowerVA { get; set; }

    [JsonPropertyName("outputVoltageV")]
    public double? OutputVoltageV { get; set; }

    [JsonPropertyName("outputCurrentA")]
    public double? OutputCurrentA { get; set; }

    [JsonPropertyName("outputFrequencyHz")]
    public double? OutputFrequencyHz { get; set; }

    // --- Grid-side view (import/export) ---
    [JsonPropertyName("gridSideActivePowerW")]
    public int? GridSideActivePowerW { get; set; }

    [JsonPropertyName("gridSidePhaseAPowerW")]
    public int? GridSidePhaseAPowerW { get; set; }

    [JsonPropertyName("gridSidePhaseBPowerW")]
    public int? GridSidePhaseBPowerW { get; set; }

    [JsonPropertyName("gridSidePhaseCPowerW")]
    public int? GridSidePhaseCPowerW { get; set; }

    // --- Temperatures ---
    [JsonPropertyName("inverterHeatsinkTempC")]
    public double? InverterHeatsinkTempC { get; set; }

    [JsonPropertyName("internalAmbientTempC")]
    public double? InternalAmbientTempC { get; set; }

    [JsonPropertyName("acInputStatus")]
    public AcInputStatusDto? AcInputStatus { get; set; }
}

public class AcInputStatusDto
{
    [JsonPropertyName("genGridSignalRaw")]
    public int? GenGridSignalRaw { get; set; }

    [JsonPropertyName("gridSignalPresent")]
    public bool GridSignalPresent { get; set; }

    [JsonPropertyName("generatorSignalPresent")]
    public bool GeneratorSignalPresent { get; set; }
}

/// <summary>
/// RPC response for getBatterySocState
/// </summary>
public class BatterySocStateDto
{
    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("inverterBatChannelSoc")]
    public List<InverterBatteryChannelRpcDto>? InverterBatChannelSoc { get; set; }

    [JsonPropertyName("bmsSocDetails")]
    public List<BmsSocDetailRpcDto>? BmsSocDetails { get; set; }
}

public class InverterBatteryChannelRpcDto
{
    [JsonPropertyName("channelId")]
    public int ChannelId { get; set; }

    [JsonPropertyName("socPercent")]
    public double? SocPercent { get; set; }
}

public class BmsSocDetailRpcDto
{
    [JsonPropertyName("channelId")]
    public int ChannelId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("socPercent")]
    public double? SocPercent { get; set; }
}

/// <summary>
/// RPC response for setExportStrategy (register 244)
/// </summary>
public class ExportStrategyStateDto
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("gatewayId")]
    public string? GatewayId { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("exportStrategy")]
    public string? ExportStrategy { get; set; } // "selling_first" | "zero_export_to_load" | "zero_export_to_ct"

    [JsonPropertyName("exportStrategyCode")]
    public int? ExportStrategyCode { get; set; } // 0, 1, 2
}

/// <summary>
/// RPC response for getDailyEnergyFlowState
/// </summary>
public class DailyEnergyFlowStateDto
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("DeviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("loadEnergyKWh")]
    public double? LoadEnergyKWh { get; set; }

    [JsonPropertyName("grid")]
    public DailyGridEnergyDto? Grid { get; set; }

    [JsonPropertyName("pv")]
    public DailyPvEnergyDto? Pv { get; set; }

    [JsonPropertyName("battery")]
    public DailyBatteryEnergyDto? Battery { get; set; }

    [JsonPropertyName("generator")]
    public DailyGeneratorEnergyDto? Generator { get; set; }
}

public class DailyGridEnergyDto
{
    [JsonPropertyName("importEnergyKWh")]
    public double? ImportEnergyKWh { get; set; }

    [JsonPropertyName("exportEnergyKWh")]
    public double? ExportEnergyKWh { get; set; }
}

public class DailyPvEnergyDto
{
    [JsonPropertyName("totalEnergyKWh")]
    public double? TotalEnergyKWh { get; set; }

    [JsonPropertyName("mppt")]
    public List<DailyMpptEnergyDto>? Mppt { get; set; }
}

public class DailyMpptEnergyDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("energyKWh")]
    public double? EnergyKWh { get; set; }
}

public class DailyBatteryEnergyDto
{
    [JsonPropertyName("chargeEnergyKWh")]
    public double? ChargeEnergyKWh { get; set; }

    [JsonPropertyName("dischargeEnergyKWh")]
    public double? DischargeEnergyKWh { get; set; }
}

public class DailyGeneratorEnergyDto
{
    [JsonPropertyName("energyKWh")]
    public double? EnergyKWh { get; set; }
}
