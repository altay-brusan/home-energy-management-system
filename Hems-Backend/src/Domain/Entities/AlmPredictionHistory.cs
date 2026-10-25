using System.ComponentModel.DataAnnotations;
using Hems_Backend.Domain.Common;
using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Stores individual prediction data points returned by the ALM service
/// </summary>
public class AlmPredictionHistory : BaseGuidAuditableEntity
{
    [Required]
    public Guid AlmPredictionJobId { get; set; }

    [Required]
    public Guid DeviceId { get; set; }

    [Required]
    public AlmModelType ModelType { get; set; }

    /// <summary>
    /// The predicted timestamp (device local time)
    /// </summary>
    [Required]
    public DateTime PredictedTime { get; set; }

    /// <summary>
    /// The predicted value (e.g., LoadTotalPowerW)
    /// </summary>
    [Required]
    public double PredictedValue { get; set; }

    /// <summary>
    /// The feature name that was predicted (e.g., "LoadTotalPowerW")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FeatureName { get; set; } = string.Empty;

    // Navigation properties
    public AlmPredictionJob PredictionJob { get; set; } = null!;
    public Device Device { get; set; } = null!;
}
