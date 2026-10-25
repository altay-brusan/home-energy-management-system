using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Synchronous device parameter update service.
/// Processes parameters sequentially with write-verify-retry pattern.
/// Replaces the async SignalR-based background service with a simple synchronous approach.
///
/// Special handling for restart-causing parameters (Grid Standard settings):
/// - These parameters are processed LAST to ensure other parameters are applied first
/// - Uses fire-and-forget pattern (no verification) since device will restart
/// - Sets DeviceWillRestart flag in result to notify UI
/// </summary>
public class DeviceParameterUpdateService : IDeviceParameterUpdateService
{
    private readonly IDeviceLockService _lockService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<DeviceSettingsHub, IDeviceSettingsHubClient> _hubContext;
    private readonly IOperationStateService _operationStateService;
    private readonly ILogger<DeviceParameterUpdateService> _logger;

    /// <summary>
    /// Maximum retry attempts for each parameter write-verify cycle
    /// </summary>
    private const int MaxRetries = 1; // One retry (total 2 attempts)

    /// <summary>
    /// Delay between write and verify read in milliseconds
    /// </summary>
    private const int WriteVerifyDelayMs = 150;

    /// <summary>
    /// Device mode value for Custom mode
    /// </summary>
    private const int CustomModeValue = 99;

    /// <summary>
    /// Regex to identify time slot end parameters (e.g., TimeSlot1End, TimeSlot2End, etc.)
    /// These parameters should NOT be sent to the device via RPC - the device handles them internally.
    /// We only save them to our local database for UI display purposes.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex TimeSlotEndRegex =
        new(@"^TimeSlot\d+End$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Hidden duplicate parameters from GeneratorSettings that overlap with GeneratorCharging.
    /// These are hidden in the UI (Visible=false) and skipped for RPC since the "real"
    /// parameters in GeneratorCharging handle the device updates.
    /// Must match the SkippedParameters set in DeviceRpcExecutorService.
    /// </summary>
    private static readonly HashSet<string> SkippedParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "GeneratorCharge",        // Duplicate of GeneratorChargingEnabled
        "GeneratorGridCharge",    // Duplicate of GridChargingEnabled
        "GeneratorStartSOC",      // Duplicate of GeneratorChargingStartSOC
        "GeneratorChargeCurrent"  // Duplicate of GeneratorChargingCurrent
    };

    public DeviceParameterUpdateService(
        IDeviceLockService lockService,
        IServiceScopeFactory serviceScopeFactory,
        IHubContext<DeviceSettingsHub, IDeviceSettingsHubClient> hubContext,
        IOperationStateService operationStateService,
        ILogger<DeviceParameterUpdateService> logger)
    {
        _lockService = lockService;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _operationStateService = operationStateService;
        _logger = logger;
    }

    public async Task<ParameterUpdateResult> UpdateParametersAsync(
        Guid deviceId,
        Guid setupDeviceId,
        Guid thingsBoardDeviceId,
        List<ParameterUpdateItem> parameters,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var result = new ParameterUpdateResult
        {
            TotalParameters = parameters.Count
        };

        // Acquire device lock with short timeout (5 seconds)
        // If we can't get it quickly, another operation is in progress
        // NOTE: We use the passed cancellationToken here so the caller can cancel if needed during lock wait
        var lockHandle = await _lockService.AcquireLockAsync(deviceId, 5000, cancellationToken);
        if (lockHandle == null)
        {
            _logger.LogWarning(
                "Failed to acquire device lock for {DeviceId} - another update is in progress",
                deviceId);

            throw new DeviceBusyException("Another update operation is in progress for this device.");
        }

        // IMPORTANT: From this point on, we use CancellationToken.None to ensure the operation
        // completes even if the client disconnects. This guarantees that SetupDeviceParameterValue
        // stays in sync with the actual device - we never leave an operation half-done.
        // The lock will be released when the operation completes, allowing subsequent operations.
        var updateToken = CancellationToken.None;

        // Create operation for tracking
        var operationId = _operationStateService.CreateOperation(deviceId, "system", parameters);
        var deviceGroup = _hubContext.Clients.Group($"Device_{deviceId}");

        try
        {
            _logger.LogInformation(
                "Starting synchronous parameter update for device {DeviceId} with {Count} parameters (Operation: {OperationId})",
                deviceId, parameters.Count, operationId);

            // Send OperationStarted event (fire-and-forget, don't wait)
            _ = deviceGroup.OperationStarted(new OperationStartedEvent(
                operationId,
                deviceId,
                parameters.Count,
                startTime
            ));

            // Create a scope for database operations
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var rpcExecutor = scope.ServiceProvider.GetRequiredService<IDeviceRpcExecutorService>();

            // Load parameter values for database updates
            var parameterIds = parameters.Select(p => p.ParameterId).ToList();
            var parameterValues = await context.SetupDeviceParameterValues
                .Include(sdpv => sdpv.Parameter)
                .Where(sdpv =>
                    sdpv.SetupDeviceId == setupDeviceId &&
                    parameterIds.Contains(sdpv.ParameterId))
                .ToDictionaryAsync(pv => pv.ParameterId, updateToken);

            // Separate restart-causing parameters from regular parameters
            // Restart-causing parameters (Grid Standard settings) must be processed LAST
            // because they cause device restart
            var regularParameters = new List<ParameterUpdateItem>();
            var restartCausingParameters = new List<ParameterUpdateItem>();

            foreach (var param in parameters)
            {
                if (RpcMethodMapping.IsRestartCausingParameter(param.ParameterName))
                {
                    restartCausingParameters.Add(param);
                    _logger.LogInformation(
                        "🔄 Parameter {ParameterName} identified as restart-causing - will be processed last with fire-and-forget",
                        param.ParameterName);
                }
                else
                {
                    regularParameters.Add(param);
                }
            }

            if (restartCausingParameters.Count > 0)
            {
                _logger.LogInformation(
                    "📋 Parameter update contains {RestartCount} restart-causing parameter(s): [{RestartParams}]. " +
                    "Processing {RegularCount} regular parameter(s) first, then restart-causing parameters.",
                    restartCausingParameters.Count,
                    string.Join(", ", restartCausingParameters.Select(p => p.ParameterName)),
                    regularParameters.Count);

                // Set restart info in result
                result.DeviceWillRestart = true;
                result.RestartReason = "Grid Standard settings changed";
                result.RestartCausingParameters = restartCausingParameters.Select(p => p.ParameterName).ToList();
            }

            var currentIndex = 0;

            // PHASE 1: Process regular parameters sequentially with write-verify pattern
            // NOTE: We do NOT check cancellation here - operation must complete to keep DB in sync with device
            foreach (var param in regularParameters)
            {
                currentIndex++;

                // Capture old value before update (from database)
                string? oldValue = null;
                if (parameterValues.TryGetValue(param.ParameterId, out var paramValue))
                {
                    oldValue = paramValue.Value;
                }

                // Mark parameter as in-progress
                _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.InProgress, null);

                try
                {
                    // Check if this is a time slot end parameter - these should NOT be sent to device via RPC
                    // The device handles end times internally. We only save to our local database for UI display.
                    var isTimeSlotEnd = IsTimeSlotEndParameter(param.ParameterName);

                    // Check if this parameter is temporarily disabled (RPC has issues)
                    var isSkippedParameter = IsSkippedParameter(param.ParameterName);

                    if (isTimeSlotEnd || isSkippedParameter)
                    {
                        // Skip RPC for these parameters - just update local database
                        var skipReason = isTimeSlotEnd
                            ? "time slot end parameter"
                            : "temporarily disabled (GeneratorAndSmartLoadConfig RPC issue)";

                        _logger.LogInformation(
                            "⏭️ Skipping RPC for {ParameterName} ({Reason}) - saving to local database only",
                            param.ParameterName, skipReason);

                        // Update database
                        if (paramValue != null)
                        {
                            paramValue.Value = param.Value;
                            await context.SaveChangesAsync(updateToken);
                        }

                        result.SuccessCount++;
                        _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.Success, null);

                        // Record in history (marked as success, no RPC verification)
                        context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                        {
                            DeviceId = deviceId,
                            CorrelationId = operationId,
                            ParameterId = param.ParameterId,
                            ParameterName = param.ParameterName,
                            Status = DeviceSettingUpdateStatus.Success,
                            NewValue = param.Value,
                            OldValue = oldValue,
                            VerifiedValue = null, // No RPC verification for skipped parameters
                            FailedReason = null,
                            RetryAttempts = 0,
                            AttemptedAt = DateTime.UtcNow
                        });

                        // Save history record
                        await context.SaveChangesAsync(updateToken);

                        // Send ParameterProgress event (fire-and-forget)
                        _ = deviceGroup.ParameterProgress(new ParameterProgressEvent(
                            operationId,
                            deviceId,
                            param.ParameterId,
                            param.ParameterName,
                            ParameterUpdateStatus.Success,
                            null,
                            currentIndex,
                            parameters.Count,
                            result.SuccessCount,
                            result.FailedCount
                        ));

                        continue; // Move to next parameter
                    }

                    // Normal parameter - execute RPC with write-verify pattern
                    var updateResult = await TryUpdateParameterAsync(
                        rpcExecutor,
                        thingsBoardDeviceId,
                        param,
                        updateToken);

                    if (updateResult.success)
                    {
                        // Update database
                        if (paramValue != null)
                        {
                            paramValue.Value = param.Value;
                            await context.SaveChangesAsync(updateToken);
                        }

                        result.SuccessCount++;
                        _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.Success, null);

                        _logger.LogDebug(
                            "Successfully updated parameter {ParameterName} on device {DeviceId}",
                            param.ParameterName, deviceId);

                        // Record successful update in history
                        context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                        {
                            DeviceId = deviceId,
                            CorrelationId = operationId,
                            ParameterId = param.ParameterId,
                            ParameterName = param.ParameterName,
                            Status = DeviceSettingUpdateStatus.Success,
                            NewValue = param.Value,
                            OldValue = oldValue,
                            VerifiedValue = updateResult.verifiedValue,
                            RetryAttempts = updateResult.retryAttempts,
                            AttemptedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedParameters.Add(new FailedParameterDetail
                        {
                            ParameterId = param.ParameterId,
                            ParameterName = param.ParameterName,
                            ErrorMessage = updateResult.errorMessage ?? "Update failed"
                        });
                        _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.Failed, updateResult.errorMessage);

                        _logger.LogWarning(
                            "Failed to update parameter {ParameterName} on device {DeviceId}: {Error}",
                            param.ParameterName, deviceId, updateResult.errorMessage);

                        // Record failed update in history
                        context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                        {
                            DeviceId = deviceId,
                            CorrelationId = operationId,
                            ParameterId = param.ParameterId,
                            ParameterName = param.ParameterName,
                            Status = DeviceSettingUpdateStatus.Failed,
                            NewValue = param.Value,
                            OldValue = oldValue,
                            VerifiedValue = updateResult.verifiedValue,
                            FailedReason = updateResult.errorMessage,
                            RetryAttempts = updateResult.retryAttempts,
                            AttemptedAt = DateTime.UtcNow
                        });
                    }

                    // Save history record
                    await context.SaveChangesAsync(updateToken);

                    // Send ParameterProgress event (fire-and-forget)
                    _ = deviceGroup.ParameterProgress(new ParameterProgressEvent(
                        operationId,
                        deviceId,
                        param.ParameterId,
                        param.ParameterName,
                        updateResult.success ? ParameterUpdateStatus.Success : ParameterUpdateStatus.Failed,
                        updateResult.errorMessage,
                        currentIndex,
                        parameters.Count,
                        result.SuccessCount,
                        result.FailedCount
                    ));
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedParameters.Add(new FailedParameterDetail
                    {
                        ParameterId = param.ParameterId,
                        ParameterName = param.ParameterName,
                        ErrorMessage = ex.Message
                    });
                    _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.Failed, ex.Message);

                    // Record failed update in history (exception case)
                    context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                    {
                        DeviceId = deviceId,
                        CorrelationId = operationId,
                        ParameterId = param.ParameterId,
                        ParameterName = param.ParameterName,
                        Status = DeviceSettingUpdateStatus.Failed,
                        NewValue = param.Value,
                        OldValue = oldValue,
                        FailedReason = ex.Message,
                        AttemptedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync(updateToken);

                    // Send ParameterProgress event for failure (fire-and-forget)
                    _ = deviceGroup.ParameterProgress(new ParameterProgressEvent(
                        operationId,
                        deviceId,
                        param.ParameterId,
                        param.ParameterName,
                        ParameterUpdateStatus.Failed,
                        ex.Message,
                        currentIndex,
                        parameters.Count,
                        result.SuccessCount,
                        result.FailedCount
                    ));

                    _logger.LogError(ex,
                        "Exception updating parameter {ParameterName} on device {DeviceId}",
                        param.ParameterName, deviceId);
                }
            }

            // PHASE 2: Process restart-causing parameters with fire-and-forget pattern
            // These are processed LAST because device will restart after receiving them
            if (restartCausingParameters.Count > 0)
            {
                _logger.LogInformation(
                    "🔄 Starting fire-and-forget processing for {Count} restart-causing parameter(s) on device {DeviceId}",
                    restartCausingParameters.Count, deviceId);

                foreach (var param in restartCausingParameters)
                {
                    currentIndex++;

                    // Capture old value before update
                    string? oldValue = null;
                    SetupDeviceParameterValue? paramValue = null;
                    if (parameterValues.TryGetValue(param.ParameterId, out paramValue))
                    {
                        oldValue = paramValue.Value;
                    }

                    // Mark parameter as in-progress
                    _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.InProgress, null);

                    try
                    {
                        _logger.LogInformation(
                            "🔄 Fire-and-forget update for restart-causing parameter {ParameterName}={Value}",
                            param.ParameterName, param.Value);

                        // Execute fire-and-forget RPC (no verification - device will restart)
                        var fireForgetResult = await rpcExecutor.ExecuteFireAndForgetUpdateAsync(
                            thingsBoardDeviceId,
                            param.ParameterName,
                            param.Value,
                            updateToken);

                        if (fireForgetResult.Success)
                        {
                            // Update database - we trust the write was sent
                            if (paramValue != null)
                            {
                                paramValue.Value = param.Value;
                                await context.SaveChangesAsync(updateToken);
                            }

                            result.SuccessCount++;
                            _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.Success, null);

                            _logger.LogInformation(
                                "✅ Fire-and-forget RPC sent for {ParameterName} - device will restart",
                                param.ParameterName);

                            // Record in history (no verification for fire-and-forget)
                            context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                            {
                                DeviceId = deviceId,
                                CorrelationId = operationId,
                                ParameterId = param.ParameterId,
                                ParameterName = param.ParameterName,
                                Status = DeviceSettingUpdateStatus.Success,
                                NewValue = param.Value,
                                OldValue = oldValue,
                                VerifiedValue = null, // No verification for restart-causing params
                                FailedReason = null,
                                RetryAttempts = 0,
                                AttemptedAt = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            result.FailedCount++;
                            result.FailedParameters.Add(new FailedParameterDetail
                            {
                                ParameterId = param.ParameterId,
                                ParameterName = param.ParameterName,
                                ErrorMessage = fireForgetResult.ErrorMessage ?? "Fire-and-forget RPC failed"
                            });
                            _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.Failed, fireForgetResult.ErrorMessage);

                            _logger.LogWarning(
                                "❌ Fire-and-forget RPC failed for {ParameterName}: {Error}",
                                param.ParameterName, fireForgetResult.ErrorMessage);

                            // Record failed update in history
                            context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                            {
                                DeviceId = deviceId,
                                CorrelationId = operationId,
                                ParameterId = param.ParameterId,
                                ParameterName = param.ParameterName,
                                Status = DeviceSettingUpdateStatus.Failed,
                                NewValue = param.Value,
                                OldValue = oldValue,
                                VerifiedValue = null,
                                FailedReason = fireForgetResult.ErrorMessage,
                                RetryAttempts = 0,
                                AttemptedAt = DateTime.UtcNow
                            });

                            // If fire-and-forget failed, device won't restart for this param
                            // Remove it from restart-causing list
                            result.RestartCausingParameters.Remove(param.ParameterName);
                        }

                        // Save history record
                        await context.SaveChangesAsync(updateToken);

                        // Send ParameterProgress event
                        _ = deviceGroup.ParameterProgress(new ParameterProgressEvent(
                            operationId,
                            deviceId,
                            param.ParameterId,
                            param.ParameterName,
                            fireForgetResult.Success ? ParameterUpdateStatus.Success : ParameterUpdateStatus.Failed,
                            fireForgetResult.ErrorMessage,
                            currentIndex,
                            parameters.Count,
                            result.SuccessCount,
                            result.FailedCount
                        ));
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.FailedParameters.Add(new FailedParameterDetail
                        {
                            ParameterId = param.ParameterId,
                            ParameterName = param.ParameterName,
                            ErrorMessage = ex.Message
                        });
                        _operationStateService.UpdateParameterProgress(operationId, param.ParameterId, ParameterUpdateStatus.Failed, ex.Message);

                        // Record failed update in history
                        context.DeviceSettingUpdateHistories.Add(new DeviceSettingUpdateHistory
                        {
                            DeviceId = deviceId,
                            CorrelationId = operationId,
                            ParameterId = param.ParameterId,
                            ParameterName = param.ParameterName,
                            Status = DeviceSettingUpdateStatus.Failed,
                            NewValue = param.Value,
                            OldValue = oldValue,
                            FailedReason = ex.Message,
                            AttemptedAt = DateTime.UtcNow
                        });
                        await context.SaveChangesAsync(updateToken);

                        // Send ParameterProgress event for failure
                        _ = deviceGroup.ParameterProgress(new ParameterProgressEvent(
                            operationId,
                            deviceId,
                            param.ParameterId,
                            param.ParameterName,
                            ParameterUpdateStatus.Failed,
                            ex.Message,
                            currentIndex,
                            parameters.Count,
                            result.SuccessCount,
                            result.FailedCount
                        ));

                        _logger.LogError(ex,
                            "Exception in fire-and-forget update for {ParameterName} on device {DeviceId}",
                            param.ParameterName, deviceId);

                        // Remove from restart list since it failed
                        result.RestartCausingParameters.Remove(param.ParameterName);
                    }
                }

                // If no restart-causing params succeeded, clear the restart flag
                if (result.RestartCausingParameters.Count == 0)
                {
                    result.DeviceWillRestart = false;
                    result.RestartReason = null;
                    _logger.LogWarning(
                        "All restart-causing parameters failed - device will NOT restart");
                }
                else
                {
                    // Mark device as restarting for grace period (2 minutes)
                    var restartGracePeriod = TimeSpan.FromMinutes(2);
                    _lockService.MarkDeviceRestarting(deviceId, restartGracePeriod);
                    _logger.LogInformation(
                        "🔄 Device {DeviceId} marked as restarting after fire-and-forget parameters. Grace period: {GracePeriod}",
                        deviceId, restartGracePeriod);
                }
            }

            // Set device mode to Custom if any parameters were successfully updated
            if (result.SuccessCount > 0)
            {
                await SetDeviceModeToCustomAsync(context, deviceId, updateToken);
            }

            result.Success = result.FailedCount == 0;
            result.Duration = DateTime.UtcNow - startTime;

            // Record operation in DeviceOperationHistory for audit trail
            var operationHistory = new DeviceOperationHistory
            {
                DeviceId = deviceId,
                CorrelationId = operationId.ToString(),
                Status = result.Success ? "completed" : (result.SuccessCount > 0 ? "partial" : "failed"),
                TotalParameters = result.TotalParameters,
                SuccessCount = result.SuccessCount,
                FailedCount = result.FailedCount,
                SkippedCount = 0,
                FailedParametersJson = result.FailedParameters.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(result.FailedParameters)
                    : null,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow,
                Duration = result.Duration,
                WasCancelled = false
            };
            context.DeviceOperationHistories.Add(operationHistory);
            await context.SaveChangesAsync(updateToken);

            // Mark operation as completed
            _operationStateService.CompleteOperation(operationId, result.Success);

            // Send OperationCompleted event (fire-and-forget)
            _ = deviceGroup.OperationCompleted(new OperationCompletedEvent(
                operationId,
                deviceId,
                result.Success,
                result.TotalParameters,
                result.SuccessCount,
                result.FailedCount,
                result.FailedParameters.Select(fp => new FailedParameterInfo(
                    fp.ParameterId,
                    fp.ParameterName,
                    fp.ErrorMessage
                )).ToList(),
                result.Duration
            ));

            _logger.LogInformation(
                "Completed parameter update for device {DeviceId}: {Success}/{Total} succeeded, {Failed} failed in {Duration}ms",
                deviceId, result.SuccessCount, result.TotalParameters, result.FailedCount,
                result.Duration.TotalMilliseconds);

            return result;
        }
        finally
        {
            await lockHandle.DisposeAsync();
        }
    }

    /// <summary>
    /// Checks if a parameter is a time slot end parameter.
    /// Time slot end parameters (e.g., TimeSlot1End, TimeSlot2End) should NOT be sent to the device via RPC.
    /// The device handles end times internally - we only save them to our local database for UI display.
    /// </summary>
    private static bool IsTimeSlotEndParameter(string parameterName)
    {
        return TimeSlotEndRegex.IsMatch(parameterName);
    }

    /// <summary>
    /// Checks if a parameter is temporarily disabled due to RPC issues.
    /// These parameters are saved to the local database but not sent via RPC.
    /// TODO: REFACTOR - Remove this once GeneratorAndSmartLoadConfig RPC is fixed.
    /// </summary>
    private static bool IsSkippedParameter(string parameterName)
    {
        return SkippedParameters.Contains(parameterName);
    }

    /// <summary>
    /// Attempts to update a single parameter with write-verify-retry pattern
    /// </summary>
    /// <returns>
    /// success: whether the update succeeded
    /// errorMessage: error message if failed
    /// verifiedValue: the value read back from device after write (for audit)
    /// retryAttempts: number of retry attempts made
    /// </returns>
    private async Task<(bool success, string? errorMessage, string? verifiedValue, int retryAttempts)> TryUpdateParameterAsync(
        IDeviceRpcExecutorService rpcExecutor,
        Guid thingsBoardDeviceId,
        ParameterUpdateItem param,
        CancellationToken cancellationToken)
    {
        string? lastError = null;
        string? lastVerifiedValue = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                _logger.LogDebug(
                    "Retry attempt {Attempt} for parameter {ParameterName}",
                    attempt, param.ParameterName);
                await Task.Delay(500, cancellationToken); // Brief delay before retry
            }

            var (success, error, verifiedValue) = await WriteAndVerifyAsync(
                rpcExecutor,
                thingsBoardDeviceId,
                param,
                cancellationToken);

            lastVerifiedValue = verifiedValue;

            if (success)
            {
                return (true, null, verifiedValue, attempt);
            }

            lastError = error;
        }

        return (false, lastError, lastVerifiedValue, MaxRetries);
    }

    /// <summary>
    /// Writes a parameter value and verifies it was written correctly
    /// </summary>
    /// <returns>
    /// success: whether the write and verify succeeded
    /// error: error message if failed
    /// verifiedValue: the value read back from the device (for audit trail)
    /// </returns>
    private async Task<(bool success, string? error, string? verifiedValue)> WriteAndVerifyAsync(
        IDeviceRpcExecutorService rpcExecutor,
        Guid thingsBoardDeviceId,
        ParameterUpdateItem param,
        CancellationToken cancellationToken)
    {
        // DEBUG: Log exact RPC call details
        _logger.LogInformation(
            "📡 RPC Write: DeviceId={ThingsBoardDeviceId}, ParameterName='{ParameterName}', Value='{Value}'",
            thingsBoardDeviceId,
            param.ParameterName,
            param.Value);

        // Step 1: Write the value
        var writeResult = await rpcExecutor.ExecuteParameterUpdateAsync(
            thingsBoardDeviceId,
            param.ParameterName,
            param.Value,
            RpcRetryOptions.Default,
            cancellationToken);

        _logger.LogInformation(
            "📡 RPC Write Result: Success={Success}, ErrorMessage='{ErrorMessage}'",
            writeResult.Success,
            writeResult.ErrorMessage ?? "none");

        if (!writeResult.Success)
        {
            return (false, $"Write failed: {writeResult.ErrorMessage}", null);
        }

        // Step 2: Wait for device to process
        await Task.Delay(WriteVerifyDelayMs, cancellationToken);

        // Step 3: Read back to verify
        var readValue = await rpcExecutor.GetParameterValueAsync(
            thingsBoardDeviceId,
            param.ParameterName,
            cancellationToken);

        _logger.LogInformation(
            "📡 RPC Verify Read: ParameterName='{ParameterName}', ReadValue='{ReadValue}'",
            param.ParameterName,
            readValue ?? "null");

        if (readValue == null)
        {
            return (false, "Read verification failed: could not read back value", null);
        }

        // Step 4: Verify value matches
        var matches = ValuesMatch(param.Value, readValue);
        _logger.LogInformation(
            "📡 RPC Verify Match: Expected='{Expected}', Actual='{Actual}', Match={Match}",
            param.Value,
            readValue,
            matches);

        if (!matches)
        {
            return (false, $"Verification failed: expected '{param.Value}', got '{readValue}'", readValue);
        }

        return (true, null, readValue);
    }

    /// <summary>
    /// Compares two values for equality, handling numeric, string, and case format comparisons
    /// </summary>
    private static bool ValuesMatch(string expected, string actual)
    {
        // Exact string match
        if (string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            return true;

        // Try PascalCase vs snake_case comparison
        // Device returns snake_case (e.g., "zero_export_to_ct") but we send PascalCase ("ZeroExportToCT")
        var normalizedExpected = NormalizeToSnakeCase(expected);
        var normalizedActual = NormalizeToSnakeCase(actual);
        if (string.Equals(normalizedExpected, normalizedActual, StringComparison.OrdinalIgnoreCase))
            return true;

        // Try numeric comparison (handles "1" vs "1.0" vs "1.00")
        if (double.TryParse(expected, out var expectedNum) && double.TryParse(actual, out var actualNum))
        {
            return Math.Abs(expectedNum - actualNum) < 0.0001;
        }

        // Try boolean comparison (handles "true" vs "1" vs "True")
        if (bool.TryParse(expected, out var expectedBool))
        {
            if (bool.TryParse(actual, out var actualBool))
                return expectedBool == actualBool;

            // Handle "1"/"0" as boolean
            if (actual == "1" || actual == "0")
                return expectedBool == (actual == "1");
        }

        return false;
    }

    /// <summary>
    /// Converts a string to snake_case format for comparison.
    /// Handles PascalCase ("ZeroExportToCT") → "zero_export_to_ct"
    /// Preserves acronyms: consecutive uppercase letters stay together (CT → ct, not c_t)
    /// Also normalizes already snake_case strings.
    /// </summary>
    private static string NormalizeToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // If already contains underscores, just lowercase it
        if (input.Contains('_'))
            return input.ToLowerInvariant();

        // Convert PascalCase/camelCase to snake_case
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c))
            {
                // Add underscore before uppercase letters only if:
                // - Not at the start
                // - Previous character was NOT uppercase (to preserve acronyms like "CT")
                if (i > 0 && !char.IsUpper(input[i - 1]))
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Sets device mode to Custom when parameters are manually updated
    /// </summary>
    private async Task SetDeviceModeToCustomAsync(
        IApplicationDbContext context,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if there's already an active Custom mode entry
            var existingCustomMode = await context.DeviceModeHistory
                .FirstOrDefaultAsync(m => m.DeviceId == deviceId && m.Mode == CustomModeValue && m.IsActive, cancellationToken);

            if (existingCustomMode != null)
            {
                _logger.LogDebug("Device {DeviceId} is already in Custom mode", deviceId);
                return;
            }

            // Deactivate all existing active modes for this device
            var existingActiveModes = await context.DeviceModeHistory
                .Where(m => m.DeviceId == deviceId && m.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existingMode in existingActiveModes)
            {
                existingMode.IsActive = false;
            }

            // Create new Custom mode history record
            var modeHistory = new DeviceModeHistory
            {
                DeviceId = deviceId,
                Mode = CustomModeValue,
                Description = "Custom Mode",
                IsActive = true
            };

            context.DeviceModeHistory.Add(modeHistory);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Device {DeviceId} mode changed to Custom due to parameter updates",
                deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to update device mode to Custom for device {DeviceId}. Parameter updates were still successful.",
                deviceId);
            // Don't throw - parameter updates were successful, mode update is a side effect
        }
    }
}

