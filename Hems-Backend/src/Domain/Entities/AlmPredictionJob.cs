using System.ComponentModel.DataAnnotations;
using Hems_Backend.Domain.Common;
using Hems_Backend.Domain.Enums;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Tracks prediction jobs submitted to the ALM service
/// </summary>
public class AlmPredictionJob : BaseGuidAuditableEntity
{
    [Required]
    public Guid DeviceId { get; set; }

    [Required]
    public Guid ThingsBoardDeviceId { get; set; }

    /// <summary>
    /// The job ID returned by the ALM service
    /// </summary>
    [MaxLength(100)]
    public string? AlmJobId { get; set; }

    [Required]
    public AlmModelType ModelType { get; set; }

    [Required]
    public AlmJobStatus Status { get; set; } = AlmJobStatus.Queued;

    /// <summary>
    /// Number of data points sent to the model
    /// </summary>
    public int DataPointCount { get; set; }

    /// <summary>
    /// Start of the data window sent to the model
    /// </summary>
    public DateTime DataStartTime { get; set; }

    /// <summary>
    /// End of the data window sent to the model
    /// </summary>
    public DateTime DataEndTime { get; set; }

    /// <summary>
    /// Error message if the job failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the job completed or failed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The user who requested the prediction
    /// </summary>
    [MaxLength(450)]
    public string? RequestedByUserId { get; set; }

    // Navigation properties
    public Device Device { get; set; } = null!;
    public ICollection<AlmPredictionHistory> PredictionResults { get; set; } = new List<AlmPredictionHistory>();
}
