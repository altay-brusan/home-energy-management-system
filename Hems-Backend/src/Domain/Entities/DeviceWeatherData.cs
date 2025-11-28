using System.ComponentModel.DataAnnotations;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Weather data associated with device locations
/// Stored separately to avoid duplication across energy samples
/// </summary>
public class DeviceWeatherData : BaseAuditableEntity
{
    [Required]
    public Guid ThingsBoardDeviceId { get; set; }
    
    [Required]
    public DateTime DataDateTime { get; set; }
    
    // Weather information
    public decimal? Temperature { get; set; }
    
    [MaxLength(100)]
    public string? TemperatureCondition { get; set; }
    
    public decimal? Humidity { get; set; }
    
    public decimal? WindSpeed { get; set; }
    
    [MaxLength(50)]
    public string? WindDirection { get; set; }
    
    public decimal? Pressure { get; set; }
    
    [MaxLength(50)]
    public string? Condition { get; set; } // "Sunny", "Cloudy", "Rainy", etc.
    
    public decimal? CloudCover { get; set; }
    
    public decimal? UvIndex { get; set; }
    
    // Navigation Properties
    public Device Device { get; set; } = null!;
}
