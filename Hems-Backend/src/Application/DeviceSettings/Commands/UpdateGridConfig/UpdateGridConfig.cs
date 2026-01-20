using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceSettings.Commands.UpdateGridConfig;

/// <summary>
/// Command to update grid configuration parameters for a device.
/// Uses fire-and-forget pattern since these parameters cause device restart.
/// Marks device as restarting after successful update.
/// </summary>
public record UpdateGridConfigCommand : IRequest<UpdateGridConfigResult>
{
    public Guid DeviceId { get; init; }
    public List<GridConfigParameterUpdate> Parameters { get; init; } = new();
}

/// <summary>
/// Single parameter update for grid config
/// </summary>
public record GridConfigParameterUpdate
{
    public Guid ParameterId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public class UpdateGridConfigCommandHandler : IRequestHandler<UpdateGridConfigCommand, UpdateGridConfigResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceLockService _deviceLockService;
    private readonly IDeviceRpcExecutorService _rpcExecutor;
    private readonly ILogger<UpdateGridConfigCommandHandler> _logger;

    /// <summary>
    /// Grace period after grid config update during which device is considered restarting.
    /// Typical device restart takes about 2 minutes.
    /// </summary>
    private static readonly TimeSpan RestartGracePeriod = TimeSpan.FromMinutes(2);

    public UpdateGridConfigCommandHandler(
        IApplicationDbContext context,
        IDeviceLockService deviceLockService,
        IDeviceRpcExecutorService rpcExecutor,
        ILogger<UpdateGridConfigCommandHandler> logger)
    {
        _context = context;
        _deviceLockService = deviceLockService;
        _rpcExecutor = rpcExecutor;
        _logger = logger;
    }

    public async Task<UpdateGridConfigResult> Handle(UpdateGridConfigCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "📥 Grid config update request: DeviceId={DeviceId}, ParameterCount={Count}",
            request.DeviceId, request.Parameters.Count);

        if (request.Parameters.Count == 0)
        {
            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "NO_PARAMETERS",
                ErrorMessage = "No parameters provided"
            };
        }

        // Check if device is restarting
        if (_deviceLockService.IsDeviceRestarting(request.DeviceId))
        {
            var restartInfo = _deviceLockService.GetRestartInfo(request.DeviceId);
            _logger.LogWarning(
                "Device {DeviceId} is currently restarting. Estimated available at: {EstimatedAvailableAt}",
                request.DeviceId, restartInfo?.EstimatedAvailableAt);

            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "DEVICE_RESTARTING",
                ErrorMessage = "Device is restarting. Please wait and try again.",
                EstimatedAvailableAt = restartInfo?.EstimatedAvailableAt,
                RestartGracePeriodSeconds = (int)(restartInfo?.RemainingTime.TotalSeconds ?? 0)
            };
        }

        // Check if device is currently locked
        if (_deviceLockService.IsDeviceLocked(request.DeviceId))
        {
            _logger.LogWarning(
                "Device {DeviceId} is currently locked - another operation is in progress",
                request.DeviceId);

            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "DEVICE_BUSY",
                ErrorMessage = "Another update operation is in progress for this device. Please wait and try again."
            };
        }

        // Find SetupDevice by DeviceId
        var setupDevice = await _context.SetupDevices
            .Include(sd => sd.Device)
            .FirstOrDefaultAsync(sd => sd.DeviceId == request.DeviceId, cancellationToken);

        if (setupDevice?.Device == null)
        {
            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "DEVICE_NOT_FOUND",
                ErrorMessage = "Device not found"
            };
        }

        // Get all parameter values for this device
        var parameterIds = request.Parameters.Select(p => p.ParameterId).ToList();
        var parameterValues = await _context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Where(sdpv =>
                sdpv.SetupDeviceId == setupDevice.Id &&
                parameterIds.Contains(sdpv.ParameterId))
            .ToDictionaryAsync(pv => pv.ParameterId, cancellationToken);

        // Validate parameters - ensure all are grid config parameters
        var validParameters = new List<(Guid ParameterId, string ParameterName, string Value, SetupDeviceParameterValue ParamValue)>();
        var invalidParameters = new List<string>();
        var notFoundParameters = new List<Guid>();

        foreach (var update in request.Parameters)
        {
            if (!parameterValues.TryGetValue(update.ParameterId, out var paramValue) || paramValue.Parameter == null)
            {
                notFoundParameters.Add(update.ParameterId);
                continue;
            }

            // Ensure parameter is a grid config parameter
            if (!RpcMethodMapping.IsGridConfigParameter(paramValue.Parameter.Name))
            {
                invalidParameters.Add(paramValue.Parameter.Name);
                _logger.LogWarning(
                    "Parameter {ParameterName} is not a grid config parameter and cannot be updated via this endpoint",
                    paramValue.Parameter.Name);
                continue;
            }

            validParameters.Add((update.ParameterId, paramValue.Parameter.Name, update.Value, paramValue));
        }

        // Return error if non-grid parameters were included
        if (invalidParameters.Count > 0)
        {
            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "INVALID_PARAMETERS",
                ErrorMessage = $"The following parameters are not grid configuration parameters and cannot be updated via this endpoint: {string.Join(", ", invalidParameters)}. Use the standard /parameters endpoint instead."
            };
        }

        // Return error if parameters not found
        if (notFoundParameters.Count > 0 && validParameters.Count == 0)
        {
            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "PARAMETERS_NOT_FOUND",
                ErrorMessage = $"No matching parameters found. {notFoundParameters.Count} parameter ID(s) were invalid for this device."
            };
        }

        if (validParameters.Count == 0)
        {
            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "NO_VALID_PARAMETERS",
                ErrorMessage = "No valid grid config parameters to update"
            };
        }

        // Acquire device lock
        var lockHandle = await _deviceLockService.AcquireLockAsync(request.DeviceId, 5000, cancellationToken);
        if (lockHandle == null)
        {
            return new UpdateGridConfigResult
            {
                Success = false,
                ErrorCode = "DEVICE_BUSY",
                ErrorMessage = "Device became busy. Please wait and try again."
            };
        }

        // Generate correlation ID for this operation
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation(
                "🔄 Starting fire-and-forget grid config update for device {DeviceId}, CorrelationId={CorrelationId}, Parameters=[{Parameters}]",
                request.DeviceId,
                correlationId,
                string.Join(", ", validParameters.Select(p => p.ParameterName)));

            var result = new UpdateGridConfigResult
            {
                TotalParameters = validParameters.Count,
                DeviceWillRestart = true,
                RestartReason = "Grid configuration changed",
                RestartGracePeriodSeconds = (int)RestartGracePeriod.TotalSeconds
            };

            // Execute fire-and-forget updates for each parameter
            foreach (var (parameterId, parameterName, value, paramValue) in validParameters)
            {
                var oldValue = paramValue.Value;

                try
                {
                    _logger.LogInformation(
                        "🔄 Fire-and-forget RPC for {ParameterName}={Value}",
                        parameterName, value);

                    var rpcResult = await _rpcExecutor.ExecuteFireAndForgetUpdateAsync(
                        setupDevice.Device.ThingsBoardDeviceId,
                        parameterName,
                        value,
                        CancellationToken.None); // Use None to ensure completion

                    if (rpcResult.Success)
                    {
                        // Update database
                        paramValue.Value = value;

                        // Record success in history
                        _context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                        {
                            DeviceId = request.DeviceId,
                            CorrelationId = correlationId,
                            ParameterId = parameterId,
                            ParameterName = parameterName,
                            Status = DeviceSettingUpdateStatus.Success,
                            NewValue = value,
                            OldValue = oldValue,
                            VerifiedValue = null, // Fire-and-forget, no verification
                            RetryAttempts = 0,
                            AttemptedAt = DateTime.UtcNow
                        });

                        result.SuccessCount++;
                        result.AppliedParameters.Add(parameterName);

                        _logger.LogInformation(
                            "✅ Fire-and-forget RPC sent for {ParameterName}",
                            parameterName);
                    }
                    else
                    {
                        // Record failure in history
                        _context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                        {
                            DeviceId = request.DeviceId,
                            CorrelationId = correlationId,
                            ParameterId = parameterId,
                            ParameterName = parameterName,
                            Status = DeviceSettingUpdateStatus.Failed,
                            NewValue = value,
                            OldValue = oldValue,
                            FailedReason = rpcResult.ErrorMessage,
                            RetryAttempts = 0,
                            AttemptedAt = DateTime.UtcNow
                        });

                        result.FailedCount++;
                        result.FailedParameters.Add(new FailedGridConfigParameter
                        {
                            ParameterId = parameterId,
                            ParameterName = parameterName,
                            ErrorMessage = rpcResult.ErrorMessage ?? "RPC failed"
                        });

                        _logger.LogWarning(
                            "❌ Fire-and-forget RPC failed for {ParameterName}: {Error}",
                            parameterName, rpcResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    // Record failure in history
                    _context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                    {
                        DeviceId = request.DeviceId,
                        CorrelationId = correlationId,
                        ParameterId = parameterId,
                        ParameterName = parameterName,
                        Status = DeviceSettingUpdateStatus.Failed,
                        NewValue = value,
                        OldValue = oldValue,
                        FailedReason = ex.Message,
                        RetryAttempts = 0,
                        AttemptedAt = DateTime.UtcNow
                    });

                    result.FailedCount++;
                    result.FailedParameters.Add(new FailedGridConfigParameter
                    {
                        ParameterId = parameterId,
                        ParameterName = parameterName,
                        ErrorMessage = ex.Message
                    });

                    _logger.LogError(ex,
                        "Exception in fire-and-forget RPC for {ParameterName}",
                        parameterName);
                }
            }

            // Save all changes (database updates + history records)
            await _context.SaveChangesAsync(CancellationToken.None);

            // If at least one parameter was successfully sent, mark device as restarting
            if (result.SuccessCount > 0)
            {
                _deviceLockService.MarkDeviceRestarting(request.DeviceId, RestartGracePeriod);

                var estimatedAvailableAt = DateTime.UtcNow.Add(RestartGracePeriod);
                result.EstimatedAvailableAt = estimatedAvailableAt;

                _logger.LogInformation(
                    "🔄 Device {DeviceId} marked as restarting. {SuccessCount} parameters applied. Estimated available at: {EstimatedAvailableAt}",
                    request.DeviceId, result.SuccessCount, estimatedAvailableAt);
            }
            else
            {
                // All parameters failed - device won't restart
                result.DeviceWillRestart = false;
                result.RestartReason = null;
            }

            result.Success = result.FailedCount == 0;
            result.Duration = DateTime.UtcNow - startTime;

            return result;
        }
        finally
        {
            await lockHandle.DisposeAsync();
        }
    }
}

/// <summary>
/// Result of UpdateGridConfig command
/// </summary>
public record UpdateGridConfigResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Total number of parameters in the request
    /// </summary>
    public int TotalParameters { get; init; }

    /// <summary>
    /// Number of parameters successfully sent to device
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of parameters that failed
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Names of parameters successfully applied
    /// </summary>
    public List<string> AppliedParameters { get; init; } = new();

    /// <summary>
    /// Details of failed parameters
    /// </summary>
    public List<FailedGridConfigParameter> FailedParameters { get; init; } = new();

    /// <summary>
    /// Total duration of the operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Indicates that the device will restart as a result of this update.
    /// When true, the device will be temporarily unavailable.
    /// </summary>
    public bool DeviceWillRestart { get; set; }

    /// <summary>
    /// Reason for device restart
    /// </summary>
    public string? RestartReason { get; set; }

    /// <summary>
    /// Grace period in seconds during which device is expected to be restarting.
    /// Frontend should disable settings UI for this duration.
    /// </summary>
    public int RestartGracePeriodSeconds { get; set; }

    /// <summary>
    /// Estimated time when the device will be available again.
    /// </summary>
    public DateTime? EstimatedAvailableAt { get; set; }
}

/// <summary>
/// Information about a failed grid config parameter update
/// </summary>
public record FailedGridConfigParameter
{
    public Guid ParameterId { get; init; }
    public string ParameterName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
