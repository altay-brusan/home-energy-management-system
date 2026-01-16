namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service interface for queuing parameter updates for background processing
/// </summary>
public interface IParameterUpdateQueueService
{
    /// <summary>
    /// Queue a parameter update request for background processing
    /// Returns immediately - use SignalR to track progress
    /// </summary>
    ValueTask QueueParameterUpdateAsync(ParameterUpdateQueueRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an in-progress parameter update operation by correlation ID
    /// </summary>
    bool CancelOperation(string correlationId);

    /// <summary>
    /// Check if an operation is currently active
    /// </summary>
    bool IsOperationActive(string correlationId);
}

/// <summary>
/// Request model for queuing parameter updates
/// </summary>
public record ParameterUpdateQueueRequest
{
    /// <summary>
    /// Unique correlation ID to track this batch operation
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Local device ID
    /// </summary>
    public Guid DeviceId { get; init; }

    /// <summary>
    /// Setup device ID for database operations
    /// </summary>
    public Guid SetupDeviceId { get; init; }

    /// <summary>
    /// ThingsBoard device ID for RPC calls
    /// </summary>
    public Guid ThingsBoardDeviceId { get; init; }

    /// <summary>
    /// List of parameters to update
    /// </summary>
    public List<ParameterUpdateQueueItem> Parameters { get; init; } = new();

    /// <summary>
    /// User ID who initiated the update
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Whether the user is an admin
    /// </summary>
    public bool IsAdmin { get; init; }
}

/// <summary>
/// Individual parameter update item
/// </summary>
public record ParameterUpdateQueueItem
{
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? RpcMethod { get; init; }
}
