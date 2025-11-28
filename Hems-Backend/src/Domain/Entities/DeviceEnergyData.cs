using System.ComponentModel.DataAnnotations;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Device energy data (cumulative kWh)
/// Stores DailyEnergyFlowState and TotalEnergyFlowState telemetry
/// </summary>
public class DeviceEnergyData : BaseAuditableEntity
{
    [Required]
    public Guid ThingsBoardDeviceId { get; set; }
    
    [Required]
    public DateTime DataDateTime { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty; // "Daily" or "Lifetime"
    
    [Required]
    [MaxLength(50)]
    public string MessageType { get; set; } = string.Empty; // "Telemetry"
    
    [Required]
    [MaxLength(50)]
    public string Kind { get; set; } = string.Empty; // "DailyEnergyFlowState" or "TotalEnergyFlowState"
    
    [Required]
    [MaxLength(20)]
    public string PhaseType { get; set; } = "SinglePhase"; // "SinglePhase" or "ThreePhase"
    
    // Load Energy (kWh)
    public double? LoadEnergyKWh { get; set; }
    
    // Grid Energy (kWh)
    public double? GridImportEnergyKWh { get; set; }
    public double? GridExportEnergyKWh { get; set; }
    
    // PV Energy (kWh) - Total and per MPPT
    public double? PvTotalEnergyKWh { get; set; }
    public double? PvMppt1EnergyKWh { get; set; }
    public double? PvMppt2EnergyKWh { get; set; }
    public double? PvMppt3EnergyKWh { get; set; }
    public double? PvMppt4EnergyKWh { get; set; }
    
    // Battery Energy (kWh)
    public double? BatteryChargeEnergyKWh { get; set; }
    public double? BatteryDischargeEnergyKWh { get; set; }
    
    // Generator Energy (kWh)
    public double? GeneratorEnergyKWh { get; set; }
    
    // Navigation Properties
    public Device Device { get; set; } = null!;
}
