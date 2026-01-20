using System.Diagnostics;
using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Common.Models.Rpc;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for executing RPC commands on devices with parameter mapping
/// Handles parameter-to-RPC method mapping, batch optimization, nested field updates,
/// and retry with exponential backoff (similar to CSMA/CD collision resolution)
/// </summary>
public class DeviceRpcExecutorService : IDeviceRpcExecutorService
{
    private readonly IThingsBoardConnectorService _tbService;
    private readonly ILogger<DeviceRpcExecutorService> _logger;
    private readonly Random _jitterRandom = new();

    // TODO: REFACTOR - These parameters are temporarily disabled due to RPC execution issues.
    // The GeneratorAndSmartLoadConfig RPC needs investigation and proper fix.
    // Remove this skip list and re-enable these parameters once the issue is resolved.
    private static readonly HashSet<string> SkippedParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "GeneratorCharge",
        "GeneratorGridCharge",
        "GeneratorStartSOC",
        "GeneratorChargeCurrent",
        "GeneratorPeakShavingEnabled",
        "GeneratorPeakShavingLimitPower",
        "SmartLoadEnabled",
        "SmartLoadOffSOC",
        "SmartLoadOnSOC",
        "SmartLoadPVPowerThreshold",
        "SmartLoadOpenDelay"
    };

    // Error messages that indicate retryable conditions
    private static readonly HashSet<string> RetryableErrorPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeout",
        "busy",
        "temporarily unavailable",
        "try again",
        "connection",
        "network"
    };

    public DeviceRpcExecutorService(
        IThingsBoardConnectorService tbService,
        ILogger<DeviceRpcExecutorService> logger)
    {
        _tbService = tbService;
        _logger = logger;
    }

    public async Task<RpcExecutionResult> ExecuteParameterUpdateAsync(
        Guid tbDeviceId,
        string parameterName,
        string value,
        RpcRetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        var options = retryOptions ?? RpcRetryOptions.Default;
        var stopwatch = Stopwatch.StartNew();

        // TODO: REFACTOR - Skip temporarily disabled parameters
        if (SkippedParameters.Contains(parameterName))
        {
            return new RpcExecutionResult
            {
                Success = true,
                ErrorMessage = $"Parameter '{parameterName}' is temporarily skipped (GeneratorAndSmartLoadConfig disabled)",
                ErrorCategory = RpcErrorCategory.ParameterSkipped,
                RetryAttempts = 0,
                TotalDurationMs = stopwatch.ElapsedMilliseconds
            };
        }

        // Get RPC method mapping
        if (!RpcMethodMapping.TryGetMapping(parameterName, out var config) || config == null)
        {
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"No RPC mapping found for parameter: {parameterName}",
                ErrorCategory = RpcErrorCategory.MappingNotFound,
                RetryAttempts = 0,
                TotalDurationMs = stopwatch.ElapsedMilliseconds
            };
        }

        // Build request outside retry loop (it doesn't change)
        var mappedValue = config.ValueMapper != null ? config.ValueMapper(value) : value;
        var request = BuildRequestObject(config.RpcKind, config.FieldPath, mappedValue);
        var requestDict = SerializeToDict(request);

        int attempt = 0;
        RpcExecutionResult? lastResult = null;

        while (attempt <= options.MaxRetries)
        {
            try
            {
                if (attempt > 0)
                {
                    // Calculate delay with exponential backoff
                    var delay = CalculateBackoffDelay(attempt, options);
                    _logger.LogInformation(
                        "Retry attempt {Attempt}/{MaxRetries} for parameter {ParameterName} on device {DeviceId} after {Delay}ms delay",
                        attempt, options.MaxRetries, parameterName, tbDeviceId, delay);

                    await Task.Delay(delay, cancellationToken);
                }

                // Send request (device will fill in undefined fields from current state)
                var updateResponse = await _tbService.SendTwoWayRpcAsync(
                    deviceId: tbDeviceId,
                    method: config.RpcMethod,
                    parameters: requestDict,
                    timeoutMs: 60000, // 60 seconds - allows time for Modbus operations + network delays
                    cancellationToken: cancellationToken
                );

                if (updateResponse != null && updateResponse.Success)
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation(
                            "Successfully updated parameter {ParameterName} on device {DeviceId} after {Attempts} retry attempt(s)",
                            parameterName, tbDeviceId, attempt);
                    }

                    return new RpcExecutionResult
                    {
                        Success = true,
                        RpcMethod = config.RpcMethod,
                        Response = updateResponse.Response,
                        ErrorCategory = RpcErrorCategory.None,
                        RetryAttempts = attempt,
                        TotalDurationMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // Determine error category and whether to retry
                var errorCategory = CategorizeError(updateResponse);
                var errorMessage = GetDetailedErrorMessage(updateResponse, config.RpcMethod);

                lastResult = new RpcExecutionResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    RpcMethod = config.RpcMethod,
                    ErrorCategory = errorCategory,
                    RetryAttempts = attempt,
                    TotalDurationMs = stopwatch.ElapsedMilliseconds
                };

                // Only retry for retryable errors
                if (!IsRetryableError(errorCategory, errorMessage))
                {
                    _logger.LogWarning(
                        "Non-retryable error for parameter {ParameterName}: {ErrorCategory} - {ErrorMessage}",
                        parameterName, errorCategory, errorMessage);
                    break;
                }

                attempt++;
            }
            catch (OperationCanceledException)
            {
                throw; // Don't retry on cancellation
            }
            catch (Exception ex)
            {
                var errorCategory = CategorizeException(ex);
                var errorMessage = $"RPC execution failed: {ex.Message}";

                lastResult = new RpcExecutionResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    RpcMethod = config.RpcMethod,
                    ErrorCategory = errorCategory,
                    RetryAttempts = attempt,
                    TotalDurationMs = stopwatch.ElapsedMilliseconds
                };

                if (!IsRetryableError(errorCategory, ex.Message))
                {
                    _logger.LogWarning(ex,
                        "Non-retryable exception for parameter {ParameterName}: {ErrorCategory}",
                        parameterName, errorCategory);
                    break;
                }

                _logger.LogWarning(ex,
                    "Retryable exception for parameter {ParameterName} (attempt {Attempt}): {ErrorCategory}",
                    parameterName, attempt, errorCategory);

                attempt++;
            }
        }

        // All retries exhausted
        if (lastResult != null && lastResult.RetryAttempts > 0)
        {
            _logger.LogError(
                "Failed to update parameter {ParameterName} on device {DeviceId} after {Attempts} attempts: {Error}",
                parameterName, tbDeviceId, lastResult.RetryAttempts, lastResult.ErrorMessage);
        }

        return lastResult ?? new RpcExecutionResult
        {
            Success = false,
            ErrorMessage = "Unknown error",
            RpcMethod = config.RpcMethod,
            ErrorCategory = RpcErrorCategory.Unknown,
            RetryAttempts = attempt,
            TotalDurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    public async Task<Dictionary<string, RpcExecutionResult>> ExecuteParameterUpdatesAsync(
        Guid tbDeviceId,
        Dictionary<string, string> parameters,
        RpcRetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        var options = retryOptions ?? RpcRetryOptions.Default;
        var results = new Dictionary<string, RpcExecutionResult>();

        // TODO: REFACTOR - Skip temporarily disabled parameters and add skip results
        foreach (var param in parameters.Where(p => SkippedParameters.Contains(p.Key)))
        {
            results[param.Key] = new RpcExecutionResult
            {
                Success = true,
                ErrorMessage = $"Parameter '{param.Key}' is temporarily skipped (GeneratorAndSmartLoadConfig disabled)",
                ErrorCategory = RpcErrorCategory.ParameterSkipped
            };
        }

        // Group parameters by RPC method to minimize calls (excluding skipped parameters)
        var parametersByMethod = parameters
            .Where(p => !SkippedParameters.Contains(p.Key)) // Filter out skipped parameters
            .Select(p => new
            {
                ParameterName = p.Key,
                Value = p.Value,
                Mapping = RpcMethodMapping.TryGetMapping(p.Key, out var config) ? config : null
            })
            .Where(p => p.Mapping != null)
            .GroupBy(p => new { p.Mapping!.RpcMethod, p.Mapping.RpcKind })
            .ToList();

        foreach (var group in parametersByMethod)
        {
            // Split into batches of 10 parameters to avoid timeout
            const int batchSize = 10;
            var paramList = group.ToList();
            var batches = paramList
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            // Process each batch with retry support
            foreach (var batch in batches)
            {
                var batchStopwatch = Stopwatch.StartNew();

                // Build request outside retry loop
                var request = CreateEmptyRequest(group.Key.RpcKind);
                foreach (var param in batch)
                {
                    var mappedValue = param.Mapping!.ValueMapper != null
                        ? param.Mapping.ValueMapper(param.Value)
                        : param.Value;
                    PopulateRequestField(request, param.Mapping.FieldPath, mappedValue);
                }
                var requestDict = SerializeToDict(request);

                int attempt = 0;
                bool batchSuccess = false;
                string? lastErrorMessage = null;
                RpcErrorCategory lastErrorCategory = RpcErrorCategory.Unknown;

                while (attempt <= options.MaxRetries && !batchSuccess)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            var delay = CalculateBackoffDelay(attempt, options);
                            _logger.LogInformation(
                                "Retry attempt {Attempt}/{MaxRetries} for batch RPC {RpcMethod} on device {DeviceId} after {Delay}ms delay",
                                attempt, options.MaxRetries, group.Key.RpcMethod, tbDeviceId, delay);

                            await Task.Delay(delay, cancellationToken);
                        }

                        // Send request (device will fill in undefined fields)
                        var updateResponse = await _tbService.SendTwoWayRpcAsync(
                            deviceId: tbDeviceId,
                            method: group.Key.RpcMethod,
                            parameters: requestDict,
                            timeoutMs: 60000, // 60 seconds - only writes changed registers
                            cancellationToken: cancellationToken
                        );

                        if (updateResponse?.Success == true)
                        {
                            batchSuccess = true;

                            // Mark results for all parameters in this batch
                            foreach (var param in batch)
                            {
                                results[param.ParameterName] = new RpcExecutionResult
                                {
                                    Success = true,
                                    RpcMethod = group.Key.RpcMethod,
                                    Response = updateResponse.Response,
                                    ErrorCategory = RpcErrorCategory.None,
                                    RetryAttempts = attempt,
                                    TotalDurationMs = batchStopwatch.ElapsedMilliseconds
                                };
                            }

                            if (attempt > 0)
                            {
                                _logger.LogInformation(
                                    "Successfully executed batch RPC {RpcMethod} on device {DeviceId} after {Attempts} retry attempt(s)",
                                    group.Key.RpcMethod, tbDeviceId, attempt);
                            }
                        }
                        else
                        {
                            lastErrorCategory = CategorizeError(updateResponse);
                            lastErrorMessage = GetDetailedErrorMessage(updateResponse, group.Key.RpcMethod);

                            if (!IsRetryableError(lastErrorCategory, lastErrorMessage))
                            {
                                _logger.LogWarning(
                                    "Non-retryable error for batch RPC {RpcMethod}: {ErrorCategory} - {ErrorMessage}",
                                    group.Key.RpcMethod, lastErrorCategory, lastErrorMessage);
                                break;
                            }

                            attempt++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        lastErrorCategory = CategorizeException(ex);
                        lastErrorMessage = $"RPC execution failed: {ex.Message}";

                        if (!IsRetryableError(lastErrorCategory, ex.Message))
                        {
                            _logger.LogWarning(ex,
                                "Non-retryable exception for batch RPC {RpcMethod}: {ErrorCategory}",
                                group.Key.RpcMethod, lastErrorCategory);
                            break;
                        }

                        _logger.LogWarning(ex,
                            "Retryable exception for batch RPC {RpcMethod} (attempt {Attempt}): {ErrorCategory}",
                            group.Key.RpcMethod, attempt, lastErrorCategory);

                        attempt++;
                    }
                }

                // If batch failed after all retries, mark all parameters as failed
                if (!batchSuccess)
                {
                    _logger.LogError(
                        "Failed batch RPC {RpcMethod} on device {DeviceId} after {Attempts} attempts: {Error}",
                        group.Key.RpcMethod, tbDeviceId, attempt, lastErrorMessage);

                    foreach (var param in batch)
                    {
                        results[param.ParameterName] = new RpcExecutionResult
                        {
                            Success = false,
                            ErrorMessage = lastErrorMessage ?? "Device did not confirm update",
                            RpcMethod = group.Key.RpcMethod,
                            ErrorCategory = lastErrorCategory,
                            RetryAttempts = attempt,
                            TotalDurationMs = batchStopwatch.ElapsedMilliseconds
                        };
                    }
                }
            }
        }

        return results;
    }

    public async Task<string?> GetParameterValueAsync(
        Guid tbDeviceId,
        string parameterName,
        CancellationToken cancellationToken = default)
    {
        if (!RpcMethodMapping.TryGetMapping(parameterName, out var config) || config == null)
        {
            return null;
        }

        try
        {
            var getCurrentParams = new Dictionary<string, object>
            {
                ["kind"] = config.RpcKind
            };

            var response = await _tbService.SendTwoWayRpcAsync(
                deviceId: tbDeviceId,
                method: config.RpcGetMethod,
                parameters: getCurrentParams,
                timeoutMs: 60000, // 60 seconds - multiple Modbus reads
                cancellationToken: cancellationToken
            );

            if (response == null || !response.Success || response.Response == null)
            {
                return null;
            }

            var configJson = JsonSerializer.Serialize(response.Response);
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);

            if (configDict == null)
            {
                return null;
            }

            var value = GetNestedValue(configDict, config.FieldPath);
            return value?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<Dictionary<string, string?>> GetParameterValuesAsync(
        Guid tbDeviceId,
        List<string> parameterNames,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string?>();

        // Group by RPC get method to minimize calls
        var parametersByMethod = parameterNames
            .Select(name => new
            {
                ParameterName = name,
                Mapping = RpcMethodMapping.TryGetMapping(name, out var config) ? config : null
            })
            .Where(p => p.Mapping != null)
            .GroupBy(p => new { p.Mapping!.RpcGetMethod, p.Mapping.RpcKind })
            .ToList();

        foreach (var group in parametersByMethod)
        {
            try
            {
                var getCurrentParams = new Dictionary<string, object>
                {
                    ["kind"] = group.Key.RpcKind
                };

                var response = await _tbService.SendTwoWayRpcAsync(
                    deviceId: tbDeviceId,
                    method: group.Key.RpcGetMethod,
                    parameters: getCurrentParams,
                    timeoutMs: 60000, // 60 seconds - multiple Modbus reads
                    cancellationToken: cancellationToken
                );

                if (response == null || !response.Success || response.Response == null)
                {
                    foreach (var param in group)
                    {
                        results[param.ParameterName] = null;
                    }
                    continue;
                }

                var configJson = JsonSerializer.Serialize(response.Response);
                var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);

                foreach (var param in group)
                {
                    var value = configDict != null ? GetNestedValue(configDict, param.Mapping!.FieldPath) : null;
                    results[param.ParameterName] = value?.ToString();
                }
            }
            catch
            {
                foreach (var param in group)
                {
                    results[param.ParameterName] = null;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Builds a strongly-typed request object with ONLY the specified field path and value.
    /// </summary>
    private static object BuildRequestObject(string rpcKind, string fieldPath, object value)
    {
        var request = CreateEmptyRequest(rpcKind);
        PopulateRequestField(request, fieldPath, value);
        return request;
    }

    /// <summary>
    /// Creates an empty request object for the specified RPC kind.
    /// </summary>
    private static object CreateEmptyRequest(string rpcKind)
    {
        return rpcKind switch
        {
            "SystemWorkModeConfigState" => new SystemWorkModeConfigRequest(),
            "BatteryChargeSourceConfigState" => new BatteryChargeSourceConfigRequest(),
            "BatteryThresholdConfigState" => new BatteryThresholdConfigRequest(),
            "GeneratorAndSmartLoadConfigState" => new GeneratorAndSmartLoadConfigRequest(),
            "BatteryMaintenanceConfigState" => new BatteryMaintenanceConfigRequest(),
            "GridCodeConfigState" => new GridCodeConfigRequest(),
            "InverterAdvancedFunctionConfigState" => new InverterAdvancedFunctionConfigRequest(),
            "ExternalRelayConfigState" => new ExternalRelayConfigRequest(),
            "GeneratorSmartLoadOverride" => new GeneratorSmartLoadOverrideRequest(),
            "SystemConnectionStructure" => new SystemConnectionStructureRequest(),
            _ => throw new NotSupportedException($"Unsupported RPC kind: {rpcKind}")
        };
    }

    /// <summary>
    /// Populates a field in a request object based on the field path.
    /// </summary>
    private static void PopulateRequestField(object request, string fieldPath, object value)
    {
        var parts = fieldPath.Split('.');

        switch (request)
        {
            case SystemWorkModeConfigRequest systemRequest:
                PopulateSystemWorkModeField(systemRequest, parts, value);
                break;
            case BatteryChargeSourceConfigRequest batteryChargeRequest:
                PopulateBatteryChargeSourceField(batteryChargeRequest, parts, value);
                break;
            case BatteryThresholdConfigRequest batteryThresholdRequest:
                PopulateBatteryThresholdField(batteryThresholdRequest, parts, value);
                break;
            case GeneratorAndSmartLoadConfigRequest generatorSmartLoadRequest:
                PopulateGeneratorAndSmartLoadField(generatorSmartLoadRequest, parts, value);
                break;
            case BatteryMaintenanceConfigRequest batteryMaintenanceRequest:
                PopulateBatteryMaintenanceField(batteryMaintenanceRequest, parts, value);
                break;
            case GridCodeConfigRequest gridCodeRequest:
                PopulateGridCodeField(gridCodeRequest, parts, value);
                break;
            case InverterAdvancedFunctionConfigRequest inverterAdvancedRequest:
                PopulateInverterAdvancedFunctionField(inverterAdvancedRequest, parts, value);
                break;
            case ExternalRelayConfigRequest externalRelayRequest:
                PopulateExternalRelayField(externalRelayRequest, parts, value);
                break;
            case GeneratorSmartLoadOverrideRequest generatorSmartLoadOverrideRequest:
                PopulateGeneratorSmartLoadOverrideField(generatorSmartLoadOverrideRequest, parts, value);
                break;
            case SystemConnectionStructureRequest systemConnectionRequest:
                PopulateSystemConnectionStructureField(systemConnectionRequest, parts, value);
                break;
            default:
                throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
        }
    }

    /// <summary>
    /// Populates a field in SystemWorkModeConfigRequest
    /// </summary>
    private static void PopulateSystemWorkModeField(SystemWorkModeConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "inverterMode":
                request.InverterMode ??= new InverterModeConfig();
                if (parts.Length >= 2)
                {
                    SetInverterModeProperty(request.InverterMode, parts[1], value);
                }
                break;

            case "energyPriority":
                request.EnergyPriority ??= new EnergyPriorityConfig();
                if (parts.Length >= 2 && parts[1] == "pattern")
                {
                    request.EnergyPriority.Pattern = value as string;
                }
                break;

            case "gridPeakShaving":
                request.GridPeakShaving ??= new GridPeakShavingConfig();
                if (parts.Length >= 2)
                {
                    SetGridPeakShavingProperty(request.GridPeakShaving, parts[1], value);
                }
                break;

            case "timeOfUse":
                request.TimeOfUse ??= new TimeOfUseConfig();
                if (parts.Length >= 2)
                {
                    SetTimeOfUseProperty(request.TimeOfUse, parts, value);
                }
                break;
        }
    }

    private static void SetInverterModeProperty(InverterModeConfig config, string property, object value)
    {
        switch (property)
        {
            case "workModeCode":
                config.WorkModeCode = Convert.ToInt32(value);
                break;
            case "exportStrategy":
                config.ExportStrategy = value as string;
                break;
            case "solarSellEnabled":
                config.SolarSellEnabled = Convert.ToBoolean(value);
                break;
            case "maxSellPowerW":
                config.MaxSellPowerW = Convert.ToInt32(value);
                break;
            case "zeroExportLimitPowerW":
                config.ZeroExportLimitPowerW = Convert.ToInt32(value);
                break;
            case "forceOffGrid":
                config.ForceOffGrid = Convert.ToBoolean(value);
                break;
        }
    }

    private static void SetGridPeakShavingProperty(GridPeakShavingConfig config, string property, object value)
    {
        switch (property)
        {
            case "enabled":
                config.Enabled = Convert.ToBoolean(value);
                break;
            case "limitPowerW":
                config.LimitPowerW = Convert.ToInt32(value);
                break;
        }
    }

    private static void SetTimeOfUseProperty(TimeOfUseConfig config, string[] parts, object value)
    {
        if (parts.Length < 2) return;

        var subSection = parts[1];

        switch (subSection)
        {
            case "enabled":
                config.Enabled = Convert.ToBoolean(value);
                break;

            case "daysActive":
                config.DaysActive ??= new DaysActiveConfig();
                if (parts.Length >= 3)
                {
                    SetDaysActiveProperty(config.DaysActive, parts[2], value);
                }
                break;

            case var s when s.StartsWith("slots"):
                var (_, arrayIndex) = ParseArrayNotation(s);
                if (arrayIndex.HasValue && parts.Length >= 3)
                {
                    config.Slots ??= new List<TimeSlotConfig>();

                    // Find existing slot with this slotIndex or create new one
                    var existingSlot = config.Slots.FirstOrDefault(slot => slot?.SlotIndex == arrayIndex.Value + 1);

                    if (existingSlot == null)
                    {
                        existingSlot = new TimeSlotConfig
                        {
                            SlotIndex = arrayIndex.Value + 1 // SlotIndex is 1-based
                        };
                        config.Slots.Add(existingSlot);
                    }

                    SetTimeSlotProperty(existingSlot, parts[2], value);
                }
                break;
        }
    }

    private static void SetDaysActiveProperty(DaysActiveConfig config, string property, object value)
    {
        var boolValue = Convert.ToBoolean(value);
        switch (property.ToLower())
        {
            case "monday":
                config.Monday = boolValue;
                break;
            case "tuesday":
                config.Tuesday = boolValue;
                break;
            case "wednesday":
                config.Wednesday = boolValue;
                break;
            case "thursday":
                config.Thursday = boolValue;
                break;
            case "friday":
                config.Friday = boolValue;
                break;
            case "saturday":
                config.Saturday = boolValue;
                break;
            case "sunday":
                config.Sunday = boolValue;
                break;
        }
    }

    private static void SetTimeSlotProperty(TimeSlotConfig config, string property, object value)
    {
        switch (property)
        {
            case "startTimeLocal":
                config.StartTimeLocal = value as string;
                break;
            case "endTimeLocal":
                // Convert "00:00" to "24:00" for end times
                // "00:00" as end time means midnight at END of day, not start
                // Device expects "24:00" to indicate full 24-hour coverage
                var endTime = value as string;
                config.EndTimeLocal = endTime == "00:00" ? "24:00" : endTime;
                break;
            case "allowGridCharge":
                config.AllowGridCharge = Convert.ToBoolean(value);
                break;
            case "allowGeneratorCharge":
                config.AllowGeneratorCharge = Convert.ToBoolean(value);
                break;
            case "maxBatteryDischargePowerW":
                config.MaxBatteryDischargePowerW = Convert.ToInt32(value);
                break;
            case "batteryThresholdSocPercent":
                config.BatteryThresholdSocPercent = Convert.ToInt32(value);
                break;
            case "batteryThresholdVoltageV":
                config.BatteryThresholdVoltageV = Convert.ToDecimal(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in BatteryChargeSourceConfigRequest
    /// </summary>
    private static void PopulateBatteryChargeSourceField(BatteryChargeSourceConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "generator":
                request.Generator ??= new GeneratorChargeConfig();
                if (parts.Length >= 2)
                {
                    SetGeneratorChargeProperty(request.Generator, parts[1], value);
                }
                break;

            case "grid":
                request.Grid ??= new GridChargeConfig();
                if (parts.Length >= 2)
                {
                    SetGridChargeProperty(request.Grid, parts[1], value);
                }
                break;
        }
    }

    private static void SetGeneratorChargeProperty(GeneratorChargeConfig config, string property, object value)
    {
        switch (property)
        {
            case "enabled":
                config.Enabled = Convert.ToBoolean(value);
                break;
            case "startSocPercent":
                config.StartSocPercent = Convert.ToInt32(value);
                break;
            case "chargeCurrentA":
                config.ChargeCurrentA = Convert.ToInt32(value);
                break;
            case "maxRunTimeHours":
                config.MaxRunTimeHours = Convert.ToDecimal(value);
                break;
            case "coolingTimeHours":
                config.CoolingTimeHours = Convert.ToDecimal(value);
                break;
        }
    }

    private static void SetGridChargeProperty(GridChargeConfig config, string property, object value)
    {
        switch (property)
        {
            case "enabled":
                config.Enabled = Convert.ToBoolean(value);
                break;
            case "startSocPercent":
                config.StartSocPercent = Convert.ToInt32(value);
                break;
            case "chargeCurrentA":
                config.ChargeCurrentA = Convert.ToInt32(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in BatteryThresholdConfigRequest
    /// </summary>
    private static void PopulateBatteryThresholdField(BatteryThresholdConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var property = parts[0];

        switch (property)
        {
            case "shutdownCapacityPercent":
                request.ShutdownCapacityPercent = Convert.ToInt32(value);
                break;
            case "restartCapacityPercent":
                request.RestartCapacityPercent = Convert.ToInt32(value);
                break;
            case "lowBatteryCapacityPercent":
                request.LowBatteryCapacityPercent = Convert.ToInt32(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in GeneratorAndSmartLoadConfigRequest
    /// </summary>
    private static void PopulateGeneratorAndSmartLoadField(GeneratorAndSmartLoadConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "generatorChargeEnabled":
                request.GeneratorChargeEnabled = Convert.ToBoolean(value);
                break;
            case "gridChargeEnabled":
                request.GridChargeEnabled = Convert.ToBoolean(value);
                break;
            case "startSocPercent":
                request.StartSocPercent = Convert.ToInt32(value);
                break;
            case "chargeCurrentA":
                request.ChargeCurrentA = Convert.ToInt32(value);
                break;
            case "generatorPeakShaving":
                request.GeneratorPeakShaving ??= new GeneratorPeakShavingConfig();
                if (parts.Length >= 2)
                {
                    SetGeneratorPeakShavingProperty(request.GeneratorPeakShaving, parts[1], value);
                }
                break;
            case "smartLoad":
                request.SmartLoad ??= new SmartLoadConfig();
                if (parts.Length >= 2)
                {
                    SetSmartLoadProperty(request.SmartLoad, parts[1], value);
                }
                break;
        }
    }

    private static void SetGeneratorPeakShavingProperty(GeneratorPeakShavingConfig config, string property, object value)
    {
        switch (property)
        {
            case "enabled":
                config.Enabled = Convert.ToBoolean(value);
                break;
            case "limitPowerW":
                config.LimitPowerW = Convert.ToInt32(value);
                break;
        }
    }

    private static void SetSmartLoadProperty(SmartLoadConfig config, string property, object value)
    {
        switch (property)
        {
            case "enabled":
                config.Enabled = Convert.ToInt32(value);
                break;
            case "offSocPercent":
                config.OffSocPercent = Convert.ToInt32(value);
                break;
            case "onSocPercent":
                config.OnSocPercent = Convert.ToInt32(value);
                break;
            case "pvPowerThresholdW":
                config.PvPowerThresholdW = Convert.ToInt32(value);
                break;
            case "openDelayMinutes":
                config.OpenDelayMinutes = Convert.ToInt32(value);
                break;
            case "onGridAlwaysOn":
                config.OnGridAlwaysOn = Convert.ToBoolean(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in BatteryMaintenanceConfigRequest
    /// </summary>
    private static void PopulateBatteryMaintenanceField(BatteryMaintenanceConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "batteryControlMode":
                request.BatteryControlMode = value as string;
                break;
            case "batteryResistanceMilliOhm":
                request.BatteryResistanceMilliOhm = Convert.ToInt32(value);
                break;
            case "chargeEfficiencyPercent":
                request.ChargeEfficiencyPercent = Convert.ToDecimal(value);
                break;
            case "wakeUp":
                request.WakeUp ??= new BatteryWakeUpConfig();
                if (parts.Length >= 2)
                {
                    SetBatteryWakeUpProperty(request.WakeUp, parts[1], value);
                }
                break;
        }
    }

    private static void SetBatteryWakeUpProperty(BatteryWakeUpConfig config, string property, object value)
    {
        switch (property)
        {
            case "channel1Enabled":
                config.Channel1Enabled = Convert.ToBoolean(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in GridCodeConfigRequest
    /// </summary>
    private static void PopulateGridCodeField(GridCodeConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "gridModeRaw":
                request.GridModeRaw = Convert.ToInt32(value);
                break;
            case "gridFrequencyHz":
                request.GridFrequencyHz = Convert.ToInt32(value);
                break;
            case "outputVoltageLevelCode":
                request.OutputVoltageLevelCode = Convert.ToInt32(value);
                break;
            case "reconnectionTimeSeconds":
                request.ReconnectionTimeSeconds = Convert.ToInt32(value);
                break;
            case "powerFactorTarget":
                request.PowerFactorTarget = Convert.ToInt32(value);
                break;
            case "activePowerRegulationPercentPerStep":
                request.ActivePowerRegulationPercentPerStep = Convert.ToDecimal(value);
                break;
            case "reactivePowerRegulationPercentPerStep":
                request.ReactivePowerRegulationPercentPerStep = Convert.ToDecimal(value);
                break;
            case "normalConnect":
                request.NormalConnect ??= new NormalConnectConfig();
                if (parts.Length >= 2)
                {
                    SetNormalConnectProperty(request.NormalConnect, parts[1], value);
                }
                break;
        }
    }

    private static void SetNormalConnectProperty(NormalConnectConfig config, string property, object value)
    {
        switch (property)
        {
            case "voltageHighV":
                config.VoltageHighV = Convert.ToDecimal(value);
                break;
            case "voltageLowV":
                config.VoltageLowV = Convert.ToDecimal(value);
                break;
            case "frequencyHighHz":
                config.FrequencyHighHz = Convert.ToDecimal(value);
                break;
            case "frequencyLowHz":
                config.FrequencyLowHz = Convert.ToDecimal(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in InverterAdvancedFunctionConfigRequest
    /// </summary>
    private static void PopulateInverterAdvancedFunctionField(InverterAdvancedFunctionConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "islandProtectionEnabled":
                request.IslandProtectionEnabled = Convert.ToBoolean(value);
                break;
            case "lithiumBatteryLossFaultEnabled":
                request.LithiumBatteryLossFaultEnabled = Convert.ToBoolean(value);
                break;
            case "arcFault":
                request.ArcFault ??= new ArcFaultConfig();
                if (parts.Length >= 2)
                {
                    SetArcFaultProperty(request.ArcFault, parts[1], value);
                }
                break;
            case "parallel":
                request.Parallel ??= new ParallelConfig();
                if (parts.Length >= 2)
                {
                    SetParallelProperty(request.Parallel, parts[1], value);
                }
                break;
            case "externalMeter":
                request.ExternalMeter ??= new ExternalMeterConfig();
                if (parts.Length >= 2)
                {
                    SetExternalMeterProperty(request.ExternalMeter, parts[1], value);
                }
                break;
        }
    }

    private static void SetArcFaultProperty(ArcFaultConfig config, string property, object value)
    {
        switch (property)
        {
            case "enabled":
                config.Enabled = Convert.ToBoolean(value);
                break;
        }
    }

    private static void SetParallelProperty(ParallelConfig config, string property, object value)
    {
        switch (property)
        {
            case "enabled":
                config.Enabled = Convert.ToBoolean(value);
                break;
            case "role":
                config.Role = value as string;
                break;
            case "phase":
                config.Phase = value as string;
                break;
            case "modbusAddress":
                config.ModbusAddress = Convert.ToInt32(value);
                break;
        }
    }

    private static void SetExternalMeterProperty(ExternalMeterConfig config, string property, object value)
    {
        switch (property)
        {
            case "ctRatio":
                config.CtRatio = Convert.ToInt32(value);
                break;
            case "meterCtRatio":
                config.MeterCtRatio = Convert.ToInt32(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in ExternalRelayConfigRequest
    /// </summary>
    private static void PopulateExternalRelayField(ExternalRelayConfigRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "externalRelayFeatureEnabled":
                request.ExternalRelayFeatureEnabled = Convert.ToBoolean(value);
                break;
            case "relayBitmapRaw":
                request.RelayBitmapRaw = Convert.ToInt32(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in GeneratorSmartLoadOverrideRequest
    /// </summary>
    private static void PopulateGeneratorSmartLoadOverrideField(GeneratorSmartLoadOverrideRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            case "forceSmartLoadOn":
                request.ForceSmartLoadOn = Convert.ToBoolean(value);
                break;
        }
    }

    /// <summary>
    /// Populates a field in SystemConnectionStructureRequest
    /// Handles both top-level fields and nested fields under inverter.acSide
    /// </summary>
    /// <summary>
    /// Populates a field in SystemConnectionStructureRequest
    /// Supports both flat structure (root-level fields) and nested paths
    /// </summary>
    private static void PopulateSystemConnectionStructureField(SystemConnectionStructureRequest request, string[] parts, object value)
    {
        if (parts.Length == 0) return;

        var section = parts[0];

        switch (section)
        {
            // Root-level fields (flat structure as per RPC spec)
            case "gridPhaseSequence":
                request.GridPhaseSequence = value;  // Can be int or string
                break;
            case "gridVoltageLevelCode":
                request.GridVoltageLevelCode = Convert.ToInt32(value);
                break;
            case "gridType":
                request.GridType = Convert.ToInt32(value);
                break;
            case "gridStandardRaw":
                request.GridStandardRaw = Convert.ToInt32(value);
                break;
            case "gridFrequencyHz":
                request.GridFrequencyHz = Convert.ToInt32(value);
                break;
            case "batteryControlMode":
                request.BatteryControlMode = value;  // Can be int or string
                break;
            case "exportLimitMode":
                request.ExportLimitMode = value;  // Can be int or string
                break;
            case "itSystemNeutralUngrounded":
                request.ItSystemNeutralUngrounded = Convert.ToBoolean(value);
                break;

            // Nested fields under generator
            case "generator":
                request.Generator ??= new SystemConnectionGeneratorConfig();
                if (parts.Length >= 2)
                {
                    SetSystemConnectionGeneratorProperty(request.Generator, parts[1], value);
                }
                break;

            // Nested fields under acSide (direct child of root)
            case "acSide":
                request.AcSide ??= new SystemConnectionAcSideConfig();
                if (parts.Length >= 2)
                {
                    SetSystemConnectionAcSideProperty(request.AcSide, parts[1], value);
                }
                break;

            // Nested fields under inverter.acSide (for backwards compatibility)
            case "inverter":
                request.Inverter ??= new SystemConnectionInverterConfig();
                if (parts.Length >= 2 && parts[1] == "acSide")
                {
                    request.Inverter.AcSide ??= new SystemConnectionAcSideConfig();
                    if (parts.Length >= 3)
                    {
                        SetSystemConnectionAcSideProperty(request.Inverter.AcSide, parts[2], value);
                    }
                }
                break;
        }
    }

    private static void SetSystemConnectionGeneratorProperty(SystemConnectionGeneratorConfig generator, string propertyName, object value)
    {
        switch (propertyName)
        {
            case "inputMode":
                generator.InputMode = value;  // Can be int or string
                break;
            case "connectedToGridInput":
                generator.ConnectedToGridInput = Convert.ToBoolean(value);
                break;
        }
    }

    private static void SetSystemConnectionAcSideProperty(SystemConnectionAcSideConfig acSide, string propertyName, object value)
    {
        switch (propertyName)
        {
            case "itSystemNeutralUngrounded":
                acSide.ItSystemNeutralUngrounded = Convert.ToBoolean(value);
                break;
            case "gridPhaseSequence":
                acSide.GridPhaseSequence = value;  // Can be int or string
                break;
            case "gridVoltageLevelCode":
                acSide.GridVoltageLevelCode = Convert.ToInt32(value);
                break;
        }
    }


    /// <summary>
    /// Serializes a strongly-typed request object to Dictionary<string, object> for RPC calls.
    /// Null properties are automatically excluded thanks to JsonIgnore attributes.
    /// Converts JsonElements to proper types for nested objects and arrays.
    /// </summary>
    private static Dictionary<string, object> SerializeToDict(object request)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        return ConvertJsonElementToDict(jsonElement);
    }

    /// <summary>
    /// Converts JsonElement to Dictionary<string, object> with proper nested object handling
    /// </summary>
    private static Dictionary<string, object> ConvertJsonElementToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElementToValue(property.Value);
        }

        return dict;
    }

    /// <summary>
    /// Converts JsonElement to appropriate C# types
    /// </summary>
    private static object ConvertJsonElementToValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = ConvertJsonElementToValue(property.Value);
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElementToValue(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString() ?? "";

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetDecimal(out var decimalValue))
                    return decimalValue;
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null!;

            default:
                return element.ToString() ?? "";
        }
    }

    /// <summary>
    /// Parses array notation from a path part (e.g., "slots[2]" returns ("slots", 2))
    /// </summary>
    private static (string propertyName, int? arrayIndex) ParseArrayNotation(string pathPart)
    {
        var openBracket = pathPart.IndexOf('[');
        if (openBracket == -1)
        {
            return (pathPart, null);
        }

        var closeBracket = pathPart.IndexOf(']', openBracket);
        if (closeBracket == -1)
        {
            throw new ArgumentException($"Invalid array notation: {pathPart}");
        }

        var propertyName = pathPart.Substring(0, openBracket);
        var indexStr = pathPart.Substring(openBracket + 1, closeBracket - openBracket - 1);

        if (!int.TryParse(indexStr, out var index))
        {
            throw new ArgumentException($"Invalid array index: {indexStr}");
        }

        return (propertyName, index);
    }

    /// <summary>
    /// Gets a value from a nested path (supports array notation: propertyName[index])
    /// </summary>
    private static object? GetNestedValue(Dictionary<string, object> dict, string path)
    {
        var parts = path.Split('.');
        object? current = dict;

        foreach (var part in parts)
        {
            var (propertyName, arrayIndex) = ParseArrayNotation(part);

            if (current is Dictionary<string, object> currentDict)
            {
                if (!currentDict.TryGetValue(propertyName, out current))
                {
                    return null;
                }

                // Handle array access if index is specified
                if (arrayIndex.HasValue)
                {
                    if (current is List<object> list)
                    {
                        if (arrayIndex.Value >= 0 && arrayIndex.Value < list.Count)
                        {
                            current = list[arrayIndex.Value];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else if (current is JsonElement jsonArray && jsonArray.ValueKind == JsonValueKind.Array)
                    {
                        try
                        {
                            current = jsonArray[arrayIndex.Value];
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object &&
                    jsonElement.TryGetProperty(propertyName, out var prop))
                {
                    current = prop;

                    // Handle array access for JsonElement
                    if (arrayIndex.HasValue && prop.ValueKind == JsonValueKind.Array)
                    {
                        try
                        {
                            current = prop[arrayIndex.Value];
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    #region Retry Helper Methods

    /// <summary>
    /// Calculates the backoff delay for a retry attempt using exponential backoff with optional jitter.
    /// Similar to CSMA/CD collision resolution algorithm used in Ethernet networks.
    /// </summary>
    private int CalculateBackoffDelay(int attempt, RpcRetryOptions options)
    {
        // Calculate exponential delay: initialDelay * (multiplier ^ (attempt - 1))
        var exponentialDelay = (int)(options.InitialDelayMs * Math.Pow(options.BackoffMultiplier, attempt - 1));

        // Cap at maximum delay
        var cappedDelay = Math.Min(exponentialDelay, options.MaxDelayMs);

        // Add jitter to prevent thundering herd (similar to CSMA/CD random backoff)
        if (options.UseJitter)
        {
            // Add random jitter of ±25% of the delay
            var jitterRange = cappedDelay / 4;
            var jitter = _jitterRandom.Next(-jitterRange, jitterRange + 1);
            cappedDelay = Math.Max(options.InitialDelayMs / 2, cappedDelay + jitter);
        }

        return cappedDelay;
    }

    /// <summary>
    /// Categorizes an error based on the RPC response
    /// </summary>
    private static RpcErrorCategory CategorizeError(TbRpcResponse? response)
    {
        if (response == null)
        {
            return RpcErrorCategory.DeviceOffline;
        }

        if (response.Success)
        {
            return RpcErrorCategory.None;
        }

        var errorMessage = response.Error?.ToLowerInvariant() ?? "";

        // Check for timeout
        if (errorMessage.Contains("timeout") || errorMessage.Contains("timed out"))
        {
            return RpcErrorCategory.Timeout;
        }

        // Check for busy state
        if (errorMessage.Contains("busy") || errorMessage.Contains("in progress") ||
            errorMessage.Contains("processing"))
        {
            return RpcErrorCategory.DeviceBusy;
        }

        // Check for offline/unreachable
        if (errorMessage.Contains("offline") || errorMessage.Contains("unreachable") ||
            errorMessage.Contains("not found") || errorMessage.Contains("disconnected"))
        {
            return RpcErrorCategory.DeviceOffline;
        }

        // Check for network errors
        if (errorMessage.Contains("network") || errorMessage.Contains("connection") ||
            errorMessage.Contains("socket") || errorMessage.Contains("mqtt"))
        {
            return RpcErrorCategory.NetworkError;
        }

        // Check for validation errors
        if (errorMessage.Contains("invalid") || errorMessage.Contains("validation") ||
            errorMessage.Contains("out of range") || errorMessage.Contains("not supported"))
        {
            return RpcErrorCategory.ValidationError;
        }

        // Check for rejection
        if (errorMessage.Contains("rejected") || errorMessage.Contains("denied") ||
            errorMessage.Contains("permission") || errorMessage.Contains("not allowed"))
        {
            return RpcErrorCategory.DeviceRejected;
        }

        return RpcErrorCategory.Unknown;
    }

    /// <summary>
    /// Categorizes an exception into an error category
    /// </summary>
    private static RpcErrorCategory CategorizeException(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();

        if (ex is TimeoutException || message.Contains("timeout"))
        {
            return RpcErrorCategory.Timeout;
        }

        if (ex is OperationCanceledException)
        {
            return RpcErrorCategory.Timeout; // Treat cancellation as timeout for retry purposes
        }

        if (ex is System.Net.Http.HttpRequestException ||
            ex is System.Net.Sockets.SocketException ||
            message.Contains("network") || message.Contains("connection"))
        {
            return RpcErrorCategory.NetworkError;
        }

        if (ex is ArgumentException || ex is FormatException ||
            message.Contains("invalid") || message.Contains("format"))
        {
            return RpcErrorCategory.ValidationError;
        }

        return RpcErrorCategory.Unknown;
    }

    /// <summary>
    /// Determines if an error is retryable based on category and message
    /// </summary>
    private bool IsRetryableError(RpcErrorCategory category, string? errorMessage)
    {
        // These error categories are always retryable
        switch (category)
        {
            case RpcErrorCategory.Timeout:
            case RpcErrorCategory.DeviceBusy:
            case RpcErrorCategory.NetworkError:
                return true;

            case RpcErrorCategory.DeviceOffline:
            case RpcErrorCategory.ValidationError:
            case RpcErrorCategory.DeviceRejected:
            case RpcErrorCategory.MappingNotFound:
            case RpcErrorCategory.ParameterSkipped:
                return false;

            case RpcErrorCategory.Unknown:
            case RpcErrorCategory.None:
            default:
                break;
        }

        // For unknown errors, check message patterns
        if (!string.IsNullOrEmpty(errorMessage))
        {
            var lowerMessage = errorMessage.ToLowerInvariant();
            return RetryableErrorPatterns.Any(pattern => lowerMessage.Contains(pattern));
        }

        // Default to retryable for unknown errors (give it a chance)
        return true;
    }

    /// <summary>
    /// Gets a detailed, user-friendly error message based on the response
    /// </summary>
    private static string GetDetailedErrorMessage(TbRpcResponse? response, string rpcMethod)
    {
        if (response == null)
        {
            return $"Device did not respond to {rpcMethod} - device may be offline or unreachable";
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            return $"{rpcMethod} failed: {response.Error}";
        }

        // Try to extract error from response object
        if (response.Response != null)
        {
            try
            {
                var responseJson = JsonSerializer.Serialize(response.Response);
                if (responseJson.Length < 500) // Only include if reasonably short
                {
                    return $"Device rejected {rpcMethod}: {responseJson}";
                }
            }
            catch
            {
                // Ignore serialization errors
            }
        }

        return $"Device did not confirm {rpcMethod} update - no specific error provided";
    }

    #endregion

    #region Fire-and-Forget Methods for Restart-Causing Parameters

    /// <summary>
    /// Executes a fire-and-forget parameter update for restart-causing parameters.
    /// Only sends the write RPC without waiting for verification (device will restart).
    /// This is used for parameters like GridStandard that cause device restart.
    /// </summary>
    public async Task<RpcExecutionResult> ExecuteFireAndForgetUpdateAsync(
        Guid tbDeviceId,
        string parameterName,
        string value,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Get RPC method mapping
        if (!RpcMethodMapping.TryGetMapping(parameterName, out var config) || config == null)
        {
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"No RPC mapping found for parameter: {parameterName}",
                ErrorCategory = RpcErrorCategory.MappingNotFound,
                RetryAttempts = 0,
                TotalDurationMs = stopwatch.ElapsedMilliseconds
            };
        }

        // Verify this is actually a restart-causing method
        if (!RpcMethodMapping.IsRestartCausingMethod(config.RpcMethod))
        {
            _logger.LogWarning(
                "ExecuteFireAndForgetUpdateAsync called for non-restart-causing parameter {ParameterName} (RPC: {RpcMethod}). " +
                "This method should only be used for restart-causing parameters.",
                parameterName, config.RpcMethod);
        }

        _logger.LogInformation(
            "🔄 Fire-and-forget RPC for restart-causing parameter: DeviceId={DeviceId}, Parameter={ParameterName}, " +
            "RpcMethod={RpcMethod}, Value={Value}",
            tbDeviceId, parameterName, config.RpcMethod, value);

        // Build request
        var mappedValue = config.ValueMapper != null ? config.ValueMapper(value) : value;
        var request = BuildRequestObject(config.RpcKind, config.FieldPath, mappedValue);
        var requestDict = SerializeToDict(request);

        try
        {
            // Send request - device will restart so we only care if the write was sent
            // Use shorter timeout since we don't expect a full verification cycle
            var updateResponse = await _tbService.SendTwoWayRpcAsync(
                deviceId: tbDeviceId,
                method: config.RpcMethod,
                parameters: requestDict,
                timeoutMs: 30000, // 30 seconds - enough to send write, not wait for verify
                cancellationToken: cancellationToken
            );

            // For restart-causing parameters, we consider it successful if the RPC was accepted
            // The device may or may not respond before restarting
            if (updateResponse != null && updateResponse.Success)
            {
                _logger.LogInformation(
                    "✅ Fire-and-forget RPC sent successfully for {ParameterName}. Device will restart.",
                    parameterName);

                return new RpcExecutionResult
                {
                    Success = true,
                    RpcMethod = config.RpcMethod,
                    Response = updateResponse.Response,
                    ErrorCategory = RpcErrorCategory.None,
                    RetryAttempts = 0,
                    TotalDurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            // If we got a response but it wasn't successful, log but still return info
            var errorMessage = GetDetailedErrorMessage(updateResponse, config.RpcMethod);
            _logger.LogWarning(
                "⚠️ Fire-and-forget RPC response for {ParameterName}: {ErrorMessage}. " +
                "Device may still restart if write was partially processed.",
                parameterName, errorMessage);

            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                RpcMethod = config.RpcMethod,
                ErrorCategory = CategorizeError(updateResponse),
                RetryAttempts = 0,
                TotalDurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // For restart-causing parameters, timeout or connection loss might actually mean
            // the device is restarting, so we log but return a softer error
            var errorCategory = CategorizeException(ex);

            // If it's a timeout or network error, the device might have restarted
            if (errorCategory == RpcErrorCategory.Timeout || errorCategory == RpcErrorCategory.NetworkError)
            {
                _logger.LogInformation(
                    "🔄 Fire-and-forget RPC for {ParameterName} ended with {ErrorCategory}. " +
                    "This may indicate the device has started restarting.",
                    parameterName, errorCategory);

                // Consider this a "soft success" - the RPC was likely received before restart
                return new RpcExecutionResult
                {
                    Success = true,
                    ErrorMessage = $"RPC sent, but device may have started restarting: {ex.Message}",
                    RpcMethod = config.RpcMethod,
                    ErrorCategory = errorCategory,
                    RetryAttempts = 0,
                    TotalDurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            _logger.LogError(ex,
                "❌ Fire-and-forget RPC failed for {ParameterName}: {ErrorCategory}",
                parameterName, errorCategory);

            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"RPC execution failed: {ex.Message}",
                RpcMethod = config.RpcMethod,
                ErrorCategory = errorCategory,
                RetryAttempts = 0,
                TotalDurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    #endregion
}
