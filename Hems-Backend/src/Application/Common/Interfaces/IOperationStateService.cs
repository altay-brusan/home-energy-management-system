namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for tracking device parameter update operations.
/// Stores operation state in-memory for late-connecting/reconnecting clients.
/// </summary>
public interface IOperationStateService
{
    /// <summary>
    /// Creates a new operation and returns its ID.
    /// </summary>
    string CreateOperation(Guid deviceId, string userId, List<ParameterUpdateItem> parameters);

    /// <summary>
    /// Updates progress for a specific parameter within an operation.
    /// </summary>
    void UpdateParameterProgress(string operationId, Guid parameterId, ParameterUpdateStatus status, string? error);

    /// <summary>
    /// Marks an operation as completed.
    /// </summary>
    void CompleteOperation(string operationId, bool success);

    /// <summary>
    /// Gets the state of a specific operation.
    /// </summary>
    OperationState? GetOperation(string operationId);

    /// <summary>
    /// Gets the active (in-progress) operation for a device, if any.
    /// </summary>
    OperationState? GetActiveOperationForDevice(Guid deviceId);
}

/// <summary>
/// Status of a parameter update within an operation
/// </summary>
public enum ParameterUpdateStatus
{
    Pending,
    InProgress,
    Success,
    Failed
}

/// <summary>
/// Full state of an operation, including all parameter progress
/// </summary>
public class OperationState
{
    public string OperationId { get; set; } = string.Empty;
    public Guid DeviceId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool Success { get; set; }
    public int TotalParameters { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int CurrentIndex { get; set; }
    public List<OperationParameterState> Parameters { get; set; } = new();
}

/// <summary>
/// State of a single parameter within an operation
/// </summary>
public class OperationParameterState
{
    public Guid ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public ParameterUpdateStatus Status { get; set; } = ParameterUpdateStatus.Pending;
    public string? ErrorMessage { get; set; }
}
