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
    public double? ActivePowerKw { get; set; }
}

/// <summary>
/// Battery component summary with multiple packs
/// </summary>
public class BatterySummaryDto : ComponentSummaryDto
{
    public double? StateOfCharge { get; set; }
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
    public double? StateOfCharge { get; set; }
}
