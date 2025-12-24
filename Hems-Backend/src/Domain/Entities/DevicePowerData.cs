using System.ComponentModel.DataAnnotations;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Real-time power flow data from devices (in Watts)
/// Stores PowerFlowState telemetry - instantaneous power readings
/// </summary>
public class DevicePowerData : BaseAuditableEntity
{
    [Required]
    public Guid ThingsBoardDeviceId { get; set; }
    
    [Required]
    public DateTime DeviceLocalTime { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty; // "PowerFlow"

    [Required]
    [MaxLength(50)]
    public string MessageType { get; set; } = "Telemetry";

    [Required]
    [MaxLength(20)]
    public string PhaseType { get; set; } = "SinglePhase"; // "SinglePhase" or "ThreePhase"

    public int? Version { get; set; }
    
    // PV Power (W)
    public double? PvTotalPowerW { get; set; }
    public double? PvMppt1PowerW { get; set; }
    public double? PvMppt2PowerW { get; set; }
    public double? PvMppt3PowerW { get; set; }
    public double? PvMppt4PowerW { get; set; }
    
    // Grid Power (W) - positive = import, negative = export
    public double? GridTotalPowerW { get; set; }
    
    // Load Power (W)
    public double? LoadTotalPowerW { get; set; }
    
    // Battery Power (W) - positive = discharge, negative = charge
    public double? BatteryPowerW { get; set; }
    
    // Generator Power (W)
    public double? GeneratorTotalPowerW { get; set; }
    
    // Navigation Properties
    public Device Device { get; set; } = null!;
}
