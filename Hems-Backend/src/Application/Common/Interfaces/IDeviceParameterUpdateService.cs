namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Exception thrown when a device is busy (locked by another operation)
/// </summary>
public class DeviceBusyException : Exception
{
    public DeviceBusyException(string message) : base(message) { }
}

/// <summary>
/// Service interface for synchronous device parameter updates.
/// Replaces the async SignalR-based approach with a simple REST call that waits for completion.
/// </summary>
public interface IDeviceParameterUpdateService
{
    /// <summary>
    /// Updates device parameters synchronously.
    /// Processes each parameter sequentially: Write RPC → Verify → Retry if needed → Update DB.
    /// </summary>
    /// <param name="deviceId">Local device ID</param>
    /// <param name="setupDeviceId">Setup device ID for database operations</param>
    /// <param name="thingsBoardDeviceId">ThingsBoard device ID for RPC calls</param>
    /// <param name="parameters">List of parameters to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete result with successes and failures</returns>
    Task<ParameterUpdateResult> UpdateParametersAsync(
        Guid deviceId,
        Guid setupDeviceId,
        Guid thingsBoardDeviceId,
        List<ParameterUpdateItem> parameters,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result of a synchronous parameter update operation
/// </summary>
public class ParameterUpdateResult
{
    /// <summary>
    /// Whether all parameters were updated successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total number of parameters in the request
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
    /// Details of failed parameters
    /// </summary>
    public List<FailedParameterDetail> FailedParameters { get; set; } = new();

    /// <summary>
    /// Total duration of the operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Indicates that the device will restart as a result of this update.
    /// When true, the device will be temporarily unavailable.
    /// UI should show restart notification and poll for device online status.
    /// </summary>
    public bool DeviceWillRestart { get; set; }

    /// <summary>
    /// Reason for device restart (e.g., "Grid Standard settings changed").
    /// Only populated when DeviceWillRestart is true.
    /// </summary>
    public string? RestartReason { get; set; }

    /// <summary>
    /// List of parameter names that caused the restart.
    /// Only populated when DeviceWillRestart is true.
    /// </summary>
    public List<string> RestartCausingParameters { get; set; } = new();
}

/// <summary>
/// Details about a failed parameter update
/// </summary>
public class FailedParameterDetail
{
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Individual parameter update item
/// </summary>
public class ParameterUpdateItem
{
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
