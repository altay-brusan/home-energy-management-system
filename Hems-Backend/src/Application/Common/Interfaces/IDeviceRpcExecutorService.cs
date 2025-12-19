using Hems_Backend.Application.DeviceSettings.Models;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for executing RPC calls to devices with parameter mapping
/// Reusable across different features (device settings, device modes, etc.)
/// </summary>
public interface IDeviceRpcExecutorService
{
    /// <summary>
    /// Execute RPC to update a single parameter on a device
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="parameterName">Parameter name</param>
    /// <param name="value">New value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RPC execution result</returns>
    Task<RpcExecutionResult> ExecuteParameterUpdateAsync(
        Guid deviceId,
        string parameterName,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute RPC to update multiple parameters on a device
    /// Groups parameters by RPC method to minimize RPC calls
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="parameters">Dictionary of parameter names and values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of RPC execution results</returns>
    Task<List<RpcExecutionResult>> ExecuteParameterUpdatesAsync(
        Guid deviceId,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current value of a parameter from the device
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="parameterName">Parameter name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current value or null if failed</returns>
    Task<string?> GetParameterValueAsync(
        Guid deviceId,
        string parameterName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current values of multiple parameters from the device
    /// Groups parameters by RPC method to minimize RPC calls
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="parameterNames">List of parameter names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of parameter names and current values</returns>
    Task<Dictionary<string, string?>> GetParameterValuesAsync(
        Guid deviceId,
        List<string> parameterNames,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an RPC execution
/// </summary>
public class RpcExecutionResult
{
    /// <summary>
    /// Whether the RPC call was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// RPC method that was called
    /// </summary>
    public string RpcMethod { get; set; } = string.Empty;

    /// <summary>
    /// Parameters that were updated
    /// </summary>
    public List<string> UpdatedParameters { get; set; } = new();

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Response payload from device
    /// </summary>
    public Dictionary<string, object>? ResponsePayload { get; set; }
}
