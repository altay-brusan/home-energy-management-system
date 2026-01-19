using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Tracks individual device parameter update attempts.
/// Each record represents a single parameter update (success or failure).
/// Used for detailed audit trail and troubleshooting failed updates.
/// </summary>
public class DeviceSettingUpdateHistory : BaseGuidAuditableEntity
{
    /// <summary>
    /// The device ID this parameter update was performed on
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Correlation ID that groups all parameter updates from the same operation.
    /// Links to DeviceOperationHistory.CorrelationId for operation-level view.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The parameter ID that was updated
    /// </summary>
    public Guid ParameterId { get; set; }

    /// <summary>
    /// Human-readable parameter name (e.g., "work_mode", "battery_soc_min")
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Update status: Success, Failed, Skipped
    /// </summary>
    public DeviceSettingUpdateStatus Status { get; set; }

    /// <summary>
    /// The value that was sent to the device (new value)
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// The value that was read from the device before the update (for tracking changes)
    /// May be null if not read before update
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// The value read back from the device after write (for verification)
    /// Null if read-back verification was not performed or failed
    /// </summary>
    public string? VerifiedValue { get; set; }

    /// <summary>
    /// Error message if the update failed
    /// </summary>
    public string? FailedReason { get; set; }

    /// <summary>
    /// Number of retry attempts made for this parameter
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// When the update attempt was made
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// User ID who initiated the update (from authentication context)
    /// </summary>
    public string? UserId { get; set; }

    // Navigation property
    public Device Device { get; set; } = null!;
}

/// <summary>
/// Status of a device setting update attempt
/// </summary>
public enum DeviceSettingUpdateStatus
{
    /// <summary>
    /// Parameter was successfully updated and verified
    /// </summary>
    Success = 0,

    /// <summary>
    /// Parameter update failed (write or verification failed)
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Parameter update was skipped (e.g., due to operation cancellation)
    /// </summary>
    Skipped = 2
}
