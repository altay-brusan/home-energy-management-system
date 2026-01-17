using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Synchronous device parameter update service.
/// Processes parameters sequentially with write-verify-retry pattern.
/// Replaces the async SignalR-based background service with a simple synchronous approach.
/// </summary>
public class DeviceParameterUpdateService : IDeviceParameterUpdateService
{
    private readonly IDeviceLockService _lockService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
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

    public DeviceParameterUpdateService(
        IDeviceLockService lockService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DeviceParameterUpdateService> logger)
    {
        _lockService = lockService;
        _serviceScopeFactory = serviceScopeFactory;
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
        var lockHandle = await _lockService.AcquireLockAsync(deviceId, 5000, cancellationToken);
        if (lockHandle == null)
        {
            _logger.LogWarning(
                "Failed to acquire device lock for {DeviceId} - another update is in progress",
                deviceId);

            throw new DeviceBusyException("Another update operation is in progress for this device.");
        }

        try
        {
            _logger.LogInformation(
                "Starting synchronous parameter update for device {DeviceId} with {Count} parameters",
                deviceId, parameters.Count);

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
                .ToDictionaryAsync(pv => pv.ParameterId, cancellationToken);

            // Process each parameter sequentially
            foreach (var param in parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var updateSuccess = await TryUpdateParameterAsync(
                        rpcExecutor,
                        thingsBoardDeviceId,
                        param,
                        cancellationToken);

                    if (updateSuccess.success)
                    {
                        // Update database
                        if (parameterValues.TryGetValue(param.ParameterId, out var paramValue))
                        {
                            paramValue.Value = param.Value;
                            await context.SaveChangesAsync(cancellationToken);
                        }

                        result.SuccessCount++;
                        _logger.LogDebug(
                            "Successfully updated parameter {ParameterName} on device {DeviceId}",
                            param.ParameterName, deviceId);
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedParameters.Add(new FailedParameterDetail
                        {
                            ParameterId = param.ParameterId,
                            ParameterName = param.ParameterName,
                            ErrorMessage = updateSuccess.errorMessage ?? "Update failed"
                        });

                        _logger.LogWarning(
                            "Failed to update parameter {ParameterName} on device {DeviceId}: {Error}",
                            param.ParameterName, deviceId, updateSuccess.errorMessage);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw to exit the loop
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

                    _logger.LogError(ex,
                        "Exception updating parameter {ParameterName} on device {DeviceId}",
                        param.ParameterName, deviceId);
                }
            }

            // Set device mode to Custom if any parameters were successfully updated
            if (result.SuccessCount > 0)
            {
                await SetDeviceModeToCustomAsync(context, deviceId, cancellationToken);
            }

            result.Success = result.FailedCount == 0;
            result.Duration = DateTime.UtcNow - startTime;

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
    /// Attempts to update a single parameter with write-verify-retry pattern
    /// </summary>
    private async Task<(bool success, string? errorMessage)> TryUpdateParameterAsync(
        IDeviceRpcExecutorService rpcExecutor,
        Guid thingsBoardDeviceId,
        ParameterUpdateItem param,
        CancellationToken cancellationToken)
    {
        string? lastError = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                _logger.LogDebug(
                    "Retry attempt {Attempt} for parameter {ParameterName}",
                    attempt, param.ParameterName);
                await Task.Delay(500, cancellationToken); // Brief delay before retry
            }

            var (success, error) = await WriteAndVerifyAsync(
                rpcExecutor,
                thingsBoardDeviceId,
                param,
                cancellationToken);

            if (success)
            {
                return (true, null);
            }

            lastError = error;
        }

        return (false, lastError);
    }

    /// <summary>
    /// Writes a parameter value and verifies it was written correctly
    /// </summary>
    private async Task<(bool success, string? error)> WriteAndVerifyAsync(
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
            return (false, $"Write failed: {writeResult.ErrorMessage}");
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
            return (false, "Read verification failed: could not read back value");
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
            return (false, $"Verification failed: expected '{param.Value}', got '{readValue}'");
        }

        return (true, null);
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

