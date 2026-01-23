using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// RPC response for getBatteryInfoState - static battery info (manufacturer, serial numbers, etc.)
/// </summary>
public class BatteryInfoStateDto
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("DeviceId")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Battery manufacturer name (e.g., "PYLON")
    /// </summary>
    [JsonPropertyName("manufacturerName")]
    public string? ManufacturerName { get; set; }

    /// <summary>
    /// Number of battery modules connected
    /// </summary>
    [JsonPropertyName("moduleCount")]
    public int? ModuleCount { get; set; }

    /// <summary>
    /// List of individual BMS module info (serial numbers, firmware, etc.)
    /// </summary>
    [JsonPropertyName("bmsList")]
    public List<BmsInfoDto>? BmsList { get; set; }

    /// <summary>
    /// Additional/extension data for specifications storage
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>
/// Individual BMS module info from getBatteryInfoState RPC
/// </summary>
public class BmsInfoDto
{
    /// <summary>
    /// BMS address/index (1-based)
    /// </summary>
    [JsonPropertyName("bmsAddress")]
    public int BmsAddress { get; set; }

    /// <summary>
    /// Rated battery capacity in Ah
    /// </summary>
    [JsonPropertyName("ratedCapacityAh")]
    public double? RatedCapacityAh { get; set; }

    /// <summary>
    /// Main software version
    /// </summary>
    [JsonPropertyName("softwareVersionMain")]
    public int? SoftwareVersionMain { get; set; }

    /// <summary>
    /// Sub software version
    /// </summary>
    [JsonPropertyName("softwareVersionSub")]
    public int? SoftwareVersionSub { get; set; }

    /// <summary>
    /// Battery module serial number
    /// </summary>
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }
}

/// <summary>
/// RPC response for getBatteryEnergyFlowState
/// Contains aggregate battery data and individual BMS module details
/// </summary>
public class BatteryEnergyFlowStateDto
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("timestampUtc")]
    public string? TimestampUtc { get; set; }

    [JsonPropertyName("DeviceId")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Number of BMS modules connected
    /// </summary>
    [JsonPropertyName("bmsCount")]
    public int? BmsCount { get; set; }

    /// <summary>
    /// Aggregate state of charge percentage (0-100)
    /// </summary>
    [JsonPropertyName("socPercent")]
    public double? SocPercent { get; set; }

    /// <summary>
    /// Aggregate state of health percentage (0-100)
    /// </summary>
    [JsonPropertyName("sohPercent")]
    public double? SohPercent { get; set; }

    /// <summary>
    /// Total battery pack voltage in Volts
    /// </summary>
    [JsonPropertyName("voltageV")]
    public double? VoltageV { get; set; }

    /// <summary>
    /// Total battery current in Amps (positive = discharging, negative = charging)
    /// </summary>
    [JsonPropertyName("currentA")]
    public double? CurrentA { get; set; }

    /// <summary>
    /// Average battery temperature in Celsius
    /// </summary>
    [JsonPropertyName("temperatureC")]
    public double? TemperatureC { get; set; }

    /// <summary>
    /// List of individual BMS module details
    /// </summary>
    [JsonPropertyName("bmsList")]
    public List<BmsEnergyFlowDto>? BmsList { get; set; }
}

/// <summary>
/// Individual BMS module data from getBatteryEnergyFlowState RPC
/// </summary>
public class BmsEnergyFlowDto
{
    /// <summary>
    /// BMS address/index (1-based)
    /// </summary>
    [JsonPropertyName("bmsAddress")]
    public int BmsAddress { get; set; }

    /// <summary>
    /// Charge/discharge status code:
    /// 0 = Idle, 1 = Charging, 2 = Discharging
    /// </summary>
    [JsonPropertyName("chargeDischargeStatus")]
    public int? ChargeDischargeStatus { get; set; }

    /// <summary>
    /// BMS module current in Amps
    /// </summary>
    [JsonPropertyName("currentA")]
    public double? CurrentA { get; set; }

    /// <summary>
    /// Battery pack voltage in Volts
    /// </summary>
    [JsonPropertyName("batteryPackVoltageV")]
    public double? BatteryPackVoltageV { get; set; }

    /// <summary>
    /// Remaining battery capacity in Ah
    /// </summary>
    [JsonPropertyName("batteryRemainingCapacityAh")]
    public double? BatteryRemainingCapacityAh { get; set; }

    /// <summary>
    /// Maximum cell voltage in millivolts
    /// </summary>
    [JsonPropertyName("maxCellVoltageMv")]
    public int? MaxCellVoltageMv { get; set; }

    /// <summary>
    /// Minimum cell voltage in millivolts
    /// </summary>
    [JsonPropertyName("minCellVoltageMv")]
    public int? MinCellVoltageMv { get; set; }

    /// <summary>
    /// Average cell voltage in millivolts
    /// </summary>
    [JsonPropertyName("avgCellVoltageMv")]
    public int? AvgCellVoltageMv { get; set; }

    /// <summary>
    /// Average cell temperature in Celsius
    /// </summary>
    [JsonPropertyName("avgCellTempC")]
    public double? AvgCellTempC { get; set; }

    /// <summary>
    /// Rated battery capacity in Ah
    /// </summary>
    [JsonPropertyName("ratedCapacityAh")]
    public double? RatedCapacityAh { get; set; }

    /// <summary>
    /// Total discharge energy in Wh
    /// </summary>
    [JsonPropertyName("totalDischargeWh")]
    public double? TotalDischargeWh { get; set; }

    /// <summary>
    /// Battery cycle count
    /// </summary>
    [JsonPropertyName("cycleCount")]
    public int? CycleCount { get; set; }

    /// <summary>
    /// State of health percentage (0-100)
    /// </summary>
    [JsonPropertyName("sohPercent")]
    public double? SohPercent { get; set; }

    /// <summary>
    /// State of charge percentage (0-100)
    /// </summary>
    [JsonPropertyName("socPercent")]
    public double? SocPercent { get; set; }
}

/// <summary>
/// BMS item for API response with human-readable status text
/// </summary>
public class BmsDetailItemDto
{
    public int BmsAddress { get; set; }
    public int? ChargeDischargeStatus { get; set; }
    public string? ChargeDischargeStatusText { get; set; }
    public double? CurrentA { get; set; }
    public double? VoltageV { get; set; }
    public double? RemainingCapacityAh { get; set; }
    public int? MaxCellVoltageMv { get; set; }
    public int? MinCellVoltageMv { get; set; }
    public int? AvgCellVoltageMv { get; set; }
    public double? AvgCellTempC { get; set; }
    public double? RatedCapacityAh { get; set; }
    public double? TotalDischargeWh { get; set; }
    public int? CycleCount { get; set; }
    public double? SohPercent { get; set; }
    public double? SocPercent { get; set; }

    /// <summary>
    /// Gets human-readable charge/discharge status text
    /// </summary>
    public static string GetStatusText(int? status)
    {
        return status switch
        {
            0 => "Idle",
            1 => "Charging",
            2 => "Discharging",
            _ => "Unknown"
        };
    }
}
