using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Device'ların enerji verilerini saklayan tablo
/// </summary>
public class DeviceEnergyData : BaseAuditableEntity
{
    [Required]
    public Guid ThingsBoardDeviceId { get; set; }
    
    [Required]
    public DateTime DataDateTime { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty; // "DailyCumulativeEnergyData" veya "LifetimeEnergyData"
    
    // Enerji değerleri (kWh) - DataType'a göre günlük veya toplam değer
    public double? PvEnergyKWh { get; set; }
    public double? GridImportEnergyKWh { get; set; }
    public double? GridExportEnergyKWh { get; set; }
    public double? GeneratorEnergyKWh { get; set; }
    public double? BatteryDischargeEnergyKWh { get; set; }
    public double? BatteryChargeEnergyKWh { get; set; }
    public double? LoadEnergyKWh { get; set; }
    
    // Hava durumu bilgileri
    public decimal? WeatherTemperature { get; set; }
    public string? WeatherTemperatureCondition { get; set; }
    
    // Navigation Properties
    public Device Device { get; set; } = null!;
}
