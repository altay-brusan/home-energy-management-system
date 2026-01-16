namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending device settings update notifications via SignalR
/// </summary>
public interface IDeviceSettingsNotificationService
{
    Task NotifyParameterUpdateStartedAsync(ParameterUpdateStartedMessage message, CancellationToken cancellationToken = default);
    Task NotifyParameterUpdatedAsync(ParameterUpdatedMessage message, CancellationToken cancellationToken = default);
    Task NotifyParameterUpdateFailedAsync(ParameterUpdateFailedMessage message, CancellationToken cancellationToken = default);
    Task NotifyParameterUpdateCompletedAsync(ParameterUpdateCompletedMessage message, CancellationToken cancellationToken = default);
}

// Message DTOs for SignalR notifications
public record ParameterUpdateStartedMessage
{
    public string CorrelationId { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public int TotalParameters { get; init; }
    public DateTime StartedAt { get; init; }
}

public record ParameterUpdatedMessage
{
    public string CorrelationId { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
    public string? RpcMethod { get; init; }
    public int CurrentIndex { get; init; }
    public int TotalParameters { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ParameterUpdateFailedMessage
{
    public string CorrelationId { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string? RpcMethod { get; init; }
    public int CurrentIndex { get; init; }
    public int TotalParameters { get; init; }
    public DateTime FailedAt { get; init; }
}

public record ParameterUpdateCompletedMessage
{
    public string CorrelationId { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public int TotalParameters { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public int SkippedCount { get; init; }
    public DateTime CompletedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public bool WasCancelled { get; init; }
    public List<FailedParameterInfo> FailedParameters { get; init; } = new();
}

public record FailedParameterInfo
{
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int RetryAttempts { get; init; }
}
