using Hems_Backend.Application.Common.Interfaces;

namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Event sent when a parameter update operation starts.
/// </summary>
public record OperationStartedEvent(
    string OperationId,
    Guid DeviceId,
    int TotalParameters,
    DateTime StartedAt
);

/// <summary>
/// Event sent after each parameter is processed.
/// </summary>
public record ParameterProgressEvent(
    string OperationId,
    Guid DeviceId,
    Guid ParameterId,
    string ParameterName,
    ParameterUpdateStatus Status,
    string? ErrorMessage,
    int CurrentIndex,
    int TotalParameters,
    int SuccessCount,
    int FailedCount
);

/// <summary>
/// Event sent when the entire operation completes.
/// </summary>
public record OperationCompletedEvent(
    string OperationId,
    Guid DeviceId,
    bool Success,
    int TotalParameters,
    int SuccessCount,
    int FailedCount,
    List<FailedParameterInfo> FailedParameters,
    TimeSpan Duration
);

/// <summary>
/// Information about a failed parameter.
/// </summary>
public record FailedParameterInfo(
    Guid ParameterId,
    string ParameterName,
    string ErrorMessage
);
