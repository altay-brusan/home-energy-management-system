using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Tracks the history of device parameter update operations.
/// Persists operation results so users can see outcomes even after app restart or cache expiry.
/// Keeps last 10 operations per device for audit and troubleshooting.
/// </summary>
public class DeviceOperationHistory : BaseGuidAuditableEntity
{
    /// <summary>
    /// The device ID this operation was performed on
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Unique correlation ID for tracking this operation
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Operation status: completed, failed, cancelled
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total number of parameters in the operation
    /// </summary>
    public int TotalParameters { get; set; }

    /// <summary>
    /// Number of parameters successfully updated
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of parameters that failed to update
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of parameters skipped (due to cancellation)
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// JSON serialized array of failed parameter info
    /// Contains: parameterId, parameterName, errorMessage, retryAttempts
    /// </summary>
    public string? FailedParametersJson { get; set; }

    /// <summary>
    /// When the operation started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the operation completed
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Total duration of the operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Whether the operation was cancelled by the user
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// User ID who initiated the operation (if available)
    /// </summary>
    public string? UserId { get; set; }

    // Navigation property
    public Device Device { get; set; } = null!;
}
