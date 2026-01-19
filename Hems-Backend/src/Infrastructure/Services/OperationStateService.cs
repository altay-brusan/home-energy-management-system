using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// In-memory operation state service for tracking device parameter updates.
/// Stores state for 30 minutes after completion to allow late-connecting clients to retrieve status.
/// Uses ConcurrentDictionary for thread-safe access from multiple SignalR connections.
/// </summary>
public class OperationStateService : IOperationStateService, IDisposable
{
    private readonly ConcurrentDictionary<string, OperationState> _operations = new();
    private readonly ConcurrentDictionary<Guid, string> _activeDeviceOperations = new();
    private readonly ILogger<OperationStateService> _logger;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(30);
    private bool _disposed;

    public OperationStateService(ILogger<OperationStateService> logger)
    {
        _logger = logger;
        // Run cleanup every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredOperations, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public string CreateOperation(Guid deviceId, string userId, List<ParameterUpdateItem> parameters)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        var state = new OperationState
        {
            OperationId = operationId,
            DeviceId = deviceId,
            UserId = userId,
            StartedAt = now,
            TotalParameters = parameters.Count,
            Parameters = parameters.Select(p => new OperationParameterState
            {
                ParameterId = p.ParameterId,
                ParameterName = p.ParameterName,
                Value = p.Value,
                Status = ParameterUpdateStatus.Pending
            }).ToList()
        };

        _operations[operationId] = state;
        _activeDeviceOperations[deviceId] = operationId;

        _logger.LogInformation(
            "Created operation {OperationId} for device {DeviceId} with {Count} parameters",
            operationId, deviceId, parameters.Count);

        return operationId;
    }

    public void UpdateParameterProgress(string operationId, Guid parameterId, ParameterUpdateStatus status, string? error)
    {
        if (!_operations.TryGetValue(operationId, out var state))
        {
            _logger.LogWarning("Operation {OperationId} not found for progress update", operationId);
            return;
        }

        var param = state.Parameters.FirstOrDefault(p => p.ParameterId == parameterId);
        if (param == null)
        {
            _logger.LogWarning(
                "Parameter {ParameterId} not found in operation {OperationId}",
                parameterId, operationId);
            return;
        }

        param.Status = status;
        param.ErrorMessage = error;

        // Update counters
        if (status == ParameterUpdateStatus.Success)
        {
            state.SuccessCount++;
            state.CurrentIndex++;
        }
        else if (status == ParameterUpdateStatus.Failed)
        {
            state.FailedCount++;
            state.CurrentIndex++;
        }
        else if (status == ParameterUpdateStatus.InProgress)
        {
            // Just mark as in progress, don't increment counters
        }

        _logger.LogDebug(
            "Updated parameter {ParameterName} ({ParameterId}) to {Status} in operation {OperationId}",
            param.ParameterName, parameterId, status, operationId);
    }

    public void CompleteOperation(string operationId, bool success)
    {
        if (!_operations.TryGetValue(operationId, out var state))
        {
            _logger.LogWarning("Operation {OperationId} not found for completion", operationId);
            return;
        }

        state.IsCompleted = true;
        state.CompletedAt = DateTime.UtcNow;
        state.Success = success;

        // Remove from active device operations
        _activeDeviceOperations.TryRemove(state.DeviceId, out _);

        _logger.LogInformation(
            "Completed operation {OperationId}: Success={Success}, {SuccessCount}/{TotalParameters} succeeded",
            operationId, success, state.SuccessCount, state.TotalParameters);
    }

    public OperationState? GetOperation(string operationId)
    {
        _operations.TryGetValue(operationId, out var state);
        return state;
    }

    public OperationState? GetActiveOperationForDevice(Guid deviceId)
    {
        if (_activeDeviceOperations.TryGetValue(deviceId, out var operationId))
        {
            return GetOperation(operationId);
        }
        return null;
    }

    private void CleanupExpiredOperations(object? state)
    {
        var cutoff = DateTime.UtcNow - _ttl;
        var expiredKeys = _operations
            .Where(kvp => kvp.Value.IsCompleted && kvp.Value.CompletedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            if (_operations.TryRemove(key, out var removed))
            {
                _logger.LogDebug(
                    "Cleaned up expired operation {OperationId} (completed at {CompletedAt})",
                    key, removed.CompletedAt);
            }
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired operations", expiredKeys.Count);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cleanupTimer.Dispose();
    }
}
