using Hems_Backend.Application.DeviceSettings.Models;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for executing RPC commands on devices with parameter mapping
/// Provides reusable RPC execution logic for device parameter updates
/// </summary>
public interface IDeviceRpcExecutorService
{
    /// <summary>
    /// Updates a single device parameter via RPC
    /// </summary>
    /// <param name="tbDeviceId">ThingsBoard device ID</param>
    /// <param name="parameterName">Parameter name (e.g., "SolarSell")</param>
    /// <param name="value">New value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RPC response with success status and details</returns>
    Task<RpcExecutionResult> ExecuteParameterUpdateAsync(
        Guid tbDeviceId,
        string parameterName,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple device parameters via RPC in batch
    /// Optimizes by grouping parameters by RPC method to minimize calls
    /// </summary>
    /// <param name="tbDeviceId">ThingsBoard device ID</param>
    /// <param name="parameters">Dictionary of parameter name to value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of parameter names to their RPC execution results</returns>
    Task<Dictionary<string, RpcExecutionResult>> ExecuteParameterUpdatesAsync(
        Guid tbDeviceId,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current value of a device parameter via RPC
    /// </summary>
    /// <param name="tbDeviceId">ThingsBoard device ID</param>
    /// <param name="parameterName">Parameter name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current parameter value or null if failed</returns>
    Task<string?> GetParameterValueAsync(
        Guid tbDeviceId,
        string parameterName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current values of multiple device parameters via RPC
    /// </summary>
    /// <param name="tbDeviceId">ThingsBoard device ID</param>
    /// <param name="parameterNames">List of parameter names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of parameter names to their current values</returns>
    Task<Dictionary<string, string?>> GetParameterValuesAsync(
        Guid tbDeviceId,
        List<string> parameterNames,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an RPC execution
/// </summary>
public record RpcExecutionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? RpcMethod { get; init; }
    public object? Response { get; init; }
}
