using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models.Rpc;

/// <summary>
/// Request model for setSystemWorkModeConfig RPC
/// All properties are nullable - only set properties will be serialized
/// </summary>
public class SystemWorkModeConfigRequest
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "SystemWorkModeConfigState";

    [JsonPropertyName("inverterMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InverterModeConfig? InverterMode { get; set; }

    [JsonPropertyName("energyPriority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EnergyPriorityConfig? EnergyPriority { get; set; }

    [JsonPropertyName("gridPeakShaving")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GridPeakShavingConfig? GridPeakShaving { get; set; }

    [JsonPropertyName("timeOfUse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeOfUseConfig? TimeOfUse { get; set; }
}

public class InverterModeConfig
{
    [JsonPropertyName("workModeCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WorkModeCode { get; set; }

    [JsonPropertyName("exportStrategy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExportStrategy { get; set; }

    [JsonPropertyName("solarSellEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SolarSellEnabled { get; set; }

    [JsonPropertyName("maxSellPowerW")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxSellPowerW { get; set; }

    [JsonPropertyName("zeroExportLimitPowerW")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ZeroExportLimitPowerW { get; set; }

    [JsonPropertyName("forceOffGrid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ForceOffGrid { get; set; }
}

public class EnergyPriorityConfig
{
    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pattern { get; set; }
}

public class GridPeakShavingConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("limitPowerW")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LimitPowerW { get; set; }
}

public class TimeOfUseConfig
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }

    [JsonPropertyName("daysActive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DaysActiveConfig? DaysActive { get; set; }

    [JsonPropertyName("slots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TimeSlotConfig>? Slots { get; set; }
}

public class DaysActiveConfig
{
    [JsonPropertyName("monday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Monday { get; set; }

    [JsonPropertyName("tuesday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Tuesday { get; set; }

    [JsonPropertyName("wednesday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Wednesday { get; set; }

    [JsonPropertyName("thursday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Thursday { get; set; }

    [JsonPropertyName("friday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Friday { get; set; }

    [JsonPropertyName("saturday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Saturday { get; set; }

    [JsonPropertyName("sunday")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Sunday { get; set; }
}

public class TimeSlotConfig
{
    /// <summary>
    /// Slot index (1-6). Required for Node-RED to identify which slot to update.
    /// </summary>
    [JsonPropertyName("slotIndex")]
    public int SlotIndex { get; set; }

    [JsonPropertyName("startTimeLocal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StartTimeLocal { get; set; }

    [JsonPropertyName("endTimeLocal")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndTimeLocal { get; set; }

    [JsonPropertyName("allowGridCharge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowGridCharge { get; set; }

    [JsonPropertyName("allowGeneratorCharge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowGeneratorCharge { get; set; }

    [JsonPropertyName("maxBatteryDischargePowerW")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxBatteryDischargePowerW { get; set; }

    [JsonPropertyName("batteryThresholdSocPercent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BatteryThresholdSocPercent { get; set; }

    [JsonPropertyName("batteryThresholdVoltageV")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? BatteryThresholdVoltageV { get; set; }
}
