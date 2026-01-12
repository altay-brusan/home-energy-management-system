using Hems_Backend.Application.DeviceSettings.Models;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for executing RPC commands on devices with parameter mapping
/// Provides reusable RPC execution logic for device parameter updates
/// </summary>
public interface IDeviceRpcExecutorService
{
    /// <summary>
    /// Updates a single device parameter via RPC with retry support
    /// </summary>
    /// <param name="tbDeviceId">ThingsBoard device ID</param>
    /// <param name="parameterName">Parameter name (e.g., "SolarSell")</param>
    /// <param name="value">New value</param>
    /// <param name="retryOptions">Optional retry configuration (uses defaults if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RPC response with success status and details</returns>
    Task<RpcExecutionResult> ExecuteParameterUpdateAsync(
        Guid tbDeviceId,
        string parameterName,
        string value,
        RpcRetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple device parameters via RPC in batch with retry support
    /// Optimizes by grouping parameters by RPC method to minimize calls
    /// </summary>
    /// <param name="tbDeviceId">ThingsBoard device ID</param>
    /// <param name="parameters">Dictionary of parameter name to value</param>
    /// <param name="retryOptions">Optional retry configuration (uses defaults if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of parameter names to their RPC execution results</returns>
    Task<Dictionary<string, RpcExecutionResult>> ExecuteParameterUpdatesAsync(
        Guid tbDeviceId,
        Dictionary<string, string> parameters,
        RpcRetryOptions? retryOptions = null,
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
/// Configuration options for RPC retry behavior using exponential backoff
/// Similar to CSMA/CD collision detection in Ethernet networks
/// </summary>
public record RpcRetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Initial delay before first retry in milliseconds (default: 500ms)
    /// </summary>
    public int InitialDelayMs { get; init; } = 500;

    /// <summary>
    /// Maximum delay between retries in milliseconds (default: 8000ms)
    /// </summary>
    public int MaxDelayMs { get; init; } = 8000;

    /// <summary>
    /// Multiplier for exponential backoff (default: 2.0)
    /// Each retry delay = previous delay * multiplier
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Whether to add random jitter to delays to prevent thundering herd (default: true)
    /// </summary>
    public bool UseJitter { get; init; } = true;

    /// <summary>
    /// Default retry options for standard RPC operations
    /// </summary>
    public static RpcRetryOptions Default => new();

    /// <summary>
    /// No retry - fail immediately on first error
    /// </summary>
    public static RpcRetryOptions NoRetry => new() { MaxRetries = 0 };

    /// <summary>
    /// Aggressive retry for critical operations (5 retries, longer delays)
    /// </summary>
    public static RpcRetryOptions Aggressive => new()
    {
        MaxRetries = 5,
        InitialDelayMs = 1000,
        MaxDelayMs = 16000
    };
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

    /// <summary>
    /// Error category for better error handling and debugging
    /// </summary>
    public RpcErrorCategory ErrorCategory { get; init; } = RpcErrorCategory.None;

    /// <summary>
    /// Number of retry attempts made before this result
    /// </summary>
    public int RetryAttempts { get; init; }

    /// <summary>
    /// Total time spent including retries in milliseconds
    /// </summary>
    public long TotalDurationMs { get; init; }
}

/// <summary>
/// Categories of RPC errors for better handling and debugging
/// </summary>
public enum RpcErrorCategory
{
    /// <summary>
    /// No error occurred
    /// </summary>
    None = 0,

    /// <summary>
    /// Device did not respond within timeout period
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// Device is busy processing another request
    /// </summary>
    DeviceBusy = 2,

    /// <summary>
    /// Device is offline or unreachable
    /// </summary>
    DeviceOffline = 3,

    /// <summary>
    /// Network error during communication
    /// </summary>
    NetworkError = 4,

    /// <summary>
    /// Invalid parameter value or validation failed on device
    /// </summary>
    ValidationError = 5,

    /// <summary>
    /// Device rejected the command (permission, state, etc.)
    /// </summary>
    DeviceRejected = 6,

    /// <summary>
    /// No RPC mapping found for the parameter
    /// </summary>
    MappingNotFound = 7,

    /// <summary>
    /// Parameter is temporarily disabled/skipped
    /// </summary>
    ParameterSkipped = 8,

    /// <summary>
    /// Unknown or unclassified error
    /// </summary>
    Unknown = 99
}
