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
/// RPC response for getInverterDeviceState
/// </summary>
public class InverterDeviceStateDto
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("DeviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("runState")]
    public string? RunState { get; set; } // "standby" | "selfcheck" | "normal" | "alarm" | "fault" | "activating"

    [JsonPropertyName("output")]
    public InverterOutputDto? Output { get; set; }

    [JsonPropertyName("gridSide")]
    public GridSideDto? GridSide { get; set; }

    [JsonPropertyName("temperatures")]
    public TemperaturesDto? Temperatures { get; set; }

    [JsonPropertyName("acInputStatus")]
    public AcInputStatusDto? AcInputStatus { get; set; }

    // Convenience properties for backward compatibility
    public int? OutputActivePowerW => Output?.TotalActivePowerW;
    public int? OutputApparentPowerVA => Output?.TotalApparentPowerVA;
    public double? OutputVoltageV => Output?.PhaseA?.VoltageV;
    public double? OutputCurrentA => Output?.PhaseA?.CurrentA;
    public double? OutputFrequencyHz => Output?.FrequencyHz;
    public int? GridSideActivePowerW => GridSide?.TotalActivePowerW;
    public int? GridSidePhaseAPowerW => GridSide?.PhaseAPowerW;
    public int? GridSidePhaseBPowerW => GridSide?.PhaseBPowerW;
    public int? GridSidePhaseCPowerW => GridSide?.PhaseCPowerW;
    public double? InverterHeatsinkTempC => Temperatures?.HeatsinkTempC;
    public double? InternalAmbientTempC => Temperatures?.DcTransformerTempC;

    // Three-phase output voltage properties
    public double? OutputPhaseAVoltageV => Output?.PhaseA?.VoltageV;
    public double? OutputPhaseBVoltageV => Output?.PhaseB?.VoltageV;
    public double? OutputPhaseCVoltageV => Output?.PhaseC?.VoltageV;

    // Three-phase output current properties
    public double? OutputPhaseACurrentA => Output?.PhaseA?.CurrentA;
    public double? OutputPhaseBCurrentA => Output?.PhaseB?.CurrentA;
    public double? OutputPhaseCCurrentA => Output?.PhaseC?.CurrentA;

    // Three-phase output power properties
    public int? OutputPhaseAPowerW => Output?.PhaseA?.ActivePowerW;
    public int? OutputPhaseBPowerW => Output?.PhaseB?.ActivePowerW;
    public int? OutputPhaseCPowerW => Output?.PhaseC?.ActivePowerW;
}

public class InverterOutputDto
{
    [JsonPropertyName("totalActivePowerW")]
    public int? TotalActivePowerW { get; set; }

    [JsonPropertyName("totalApparentPowerVA")]
    public int? TotalApparentPowerVA { get; set; }

    [JsonPropertyName("frequencyHz")]
    public double? FrequencyHz { get; set; }

    [JsonPropertyName("phaseA")]
    public PhaseDataDto? PhaseA { get; set; }

    [JsonPropertyName("phaseB")]
    public PhaseDataDto? PhaseB { get; set; }

    [JsonPropertyName("phaseC")]
    public PhaseDataDto? PhaseC { get; set; }
}

public class PhaseDataDto
{
    [JsonPropertyName("voltageV")]
    public double? VoltageV { get; set; }

    [JsonPropertyName("currentA")]
    public double? CurrentA { get; set; }

    [JsonPropertyName("activePowerW")]
    public int? ActivePowerW { get; set; }
}

public class GridSideDto
{
    [JsonPropertyName("totalActivePowerW")]
    public int? TotalActivePowerW { get; set; }

    [JsonPropertyName("phaseAPowerW")]
    public int? PhaseAPowerW { get; set; }

    [JsonPropertyName("phaseBPowerW")]
    public int? PhaseBPowerW { get; set; }

    [JsonPropertyName("phaseCPowerW")]
    public int? PhaseCPowerW { get; set; }
}

public class TemperaturesDto
{
    [JsonPropertyName("dcTransformerTempC")]
    public double? DcTransformerTempC { get; set; }

    [JsonPropertyName("heatsinkTempC")]
    public double? HeatsinkTempC { get; set; }
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

/// <summary>
/// RPC response for getTotalEnergyFlowState (Lifetime/Total energy)
/// </summary>
public class TotalEnergyFlowStateDto
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("grid")]
    public TotalGridEnergyDto? Grid { get; set; }

    [JsonPropertyName("pv")]
    public TotalPvEnergyDto? Pv { get; set; }

    [JsonPropertyName("load")]
    public TotalLoadEnergyDto? Load { get; set; }

    [JsonPropertyName("battery")]
    public TotalBatteryEnergyDto? Battery { get; set; }

    [JsonPropertyName("generator")]
    public TotalGeneratorEnergyDto? Generator { get; set; }
}

public class TotalGridEnergyDto
{
    [JsonPropertyName("importTotalEnergyKWh")]
    public double? ImportTotalEnergyKWh { get; set; }

    [JsonPropertyName("exportTotalEnergyKWh")]
    public double? ExportTotalEnergyKWh { get; set; }
}

public class TotalPvEnergyDto
{
    [JsonPropertyName("totalEnergyKWh")]
    public double? TotalEnergyKWh { get; set; }

    [JsonPropertyName("mppt")]
    public List<TotalMpptEnergyDto>? Mppt { get; set; }
}

public class TotalMpptEnergyDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("totalEnergyKWh")]
    public double? TotalEnergyKWh { get; set; }
}

public class TotalLoadEnergyDto
{
    [JsonPropertyName("totalEnergyKWh")]
    public double? TotalEnergyKWh { get; set; }
}

public class TotalBatteryEnergyDto
{
    [JsonPropertyName("totalChargeEnergyKWh")]
    public double? TotalChargeEnergyKWh { get; set; }

    [JsonPropertyName("totalDischargeEnergyKWh")]
    public double? TotalDischargeEnergyKWh { get; set; }
}

public class TotalGeneratorEnergyDto
{
    [JsonPropertyName("totalEnergyKWh")]
    public double? TotalEnergyKWh { get; set; }
}

/// <summary>
/// RPC response for getLoadDeviceState
/// </summary>
public class LoadDeviceStateDto
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("DeviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; } // "backup_load" | "total_load"

    [JsonPropertyName("loadPower")]
    public LoadPowerDto? LoadPower { get; set; }

    [JsonPropertyName("loadVoltage")]
    public LoadVoltageDto? LoadVoltage { get; set; }

    [JsonPropertyName("frequencyHz")]
    public double? FrequencyHz { get; set; }

    [JsonPropertyName("upsLoadPower")]
    public UpsLoadPowerDto? UpsLoadPower { get; set; }

    [JsonPropertyName("todayEnergyKWh")]
    public double? TodayEnergyKWh { get; set; }

    [JsonPropertyName("totalEnergyKWh")]
    public double? TotalEnergyKWh { get; set; }

    // Convenience properties for backward compatibility
    public int? LoadTotalActivePowerW => LoadPower?.TotalActivePowerW;
    public int? LoadPhaseAPowerW => LoadPower?.PhaseAPowerW;
    public int? LoadPhaseBPowerW => LoadPower?.PhaseBPowerW;
    public int? LoadPhaseCPowerW => LoadPower?.PhaseCPowerW;
    public double? LoadPhaseAVoltageV => LoadVoltage?.PhaseAVoltageV;
    public double? LoadPhaseBVoltageV => LoadVoltage?.PhaseBVoltageV;
    public double? LoadPhaseCVoltageV => LoadVoltage?.PhaseCVoltageV;
}

public class LoadPowerDto
{
    [JsonPropertyName("totalActivePowerW")]
    public int? TotalActivePowerW { get; set; }

    [JsonPropertyName("phaseAPowerW")]
    public int? PhaseAPowerW { get; set; }

    [JsonPropertyName("phaseBPowerW")]
    public int? PhaseBPowerW { get; set; }

    [JsonPropertyName("phaseCPowerW")]
    public int? PhaseCPowerW { get; set; }
}

public class LoadVoltageDto
{
    [JsonPropertyName("phaseAVoltageV")]
    public double? PhaseAVoltageV { get; set; }

    [JsonPropertyName("phaseBVoltageV")]
    public double? PhaseBVoltageV { get; set; }

    [JsonPropertyName("phaseCVoltageV")]
    public double? PhaseCVoltageV { get; set; }
}

public class UpsLoadPowerDto
{
    [JsonPropertyName("totalPowerW")]
    public int? TotalPowerW { get; set; }

    [JsonPropertyName("phaseAPowerW")]
    public int? PhaseAPowerW { get; set; }

    [JsonPropertyName("phaseBPowerW")]
    public int? PhaseBPowerW { get; set; }

    [JsonPropertyName("phaseCPowerW")]
    public int? PhaseCPowerW { get; set; }
}
