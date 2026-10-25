using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Device summary response containing inverter and battery information
/// </summary>
public class DeviceSummaryResponseDto
{
    public List<ComponentSummaryDto> Components { get; set; } = new();
}

/// <summary>
/// Base component summary information
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(InverterSummaryDto), typeDiscriminator: "inverter")]
[JsonDerivedType(typeof(BatterySummaryDto), typeDiscriminator: "battery")]
public class ComponentSummaryDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // "inverter" or "battery"
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
}

/// <summary>
/// Inverter component summary
/// </summary>
public class InverterSummaryDto : ComponentSummaryDto
{
    /// <summary>
    /// Current active power output in kW (realtime from RPC)
    /// </summary>
    public double? ActivePowerKw { get; set; }
}

/// <summary>
/// Battery component summary with multiple packs
/// </summary>
public class BatterySummaryDto : ComponentSummaryDto
{
    /// <summary>
    /// Aggregate state of charge percentage (average of all packs)
    /// </summary>
    public double? StateOfCharge { get; set; }

    /// <summary>
    /// Individual battery packs with their own SOC
    /// </summary>
    public List<BatteryPackDto> Packs { get; set; } = new();
}

/// <summary>
/// Individual battery pack information
/// </summary>
public class BatteryPackDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "battery";
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Parent BMS component ID (GUID) for API calls
    /// </summary>
    public Guid ComponentId { get; set; }

    /// <summary>
    /// BMS address within the battery system (1-based)
    /// </summary>
    public int BmsAddress { get; set; }

    /// <summary>
    /// Individual pack state of charge percentage
    /// </summary>
    public double? StateOfCharge { get; set; }

    /// <summary>
    /// Individual pack state of health percentage
    /// </summary>
    public double? StateOfHealth { get; set; }

    /// <summary>
    /// Battery pack voltage in Volts
    /// </summary>
    public double? Voltage { get; set; }

    /// <summary>
    /// Battery current in Amps (positive = discharging, negative = charging)
    /// </summary>
    public double? Current { get; set; }

    /// <summary>
    /// Average cell temperature in Celsius
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Software/firmware version string
    /// </summary>
    public string? FirmwareVersion { get; set; }
}
