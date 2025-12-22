using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Common.Models.Rpc;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for executing RPC commands on devices with parameter mapping
/// Handles parameter-to-RPC method mapping, batch optimization, and nested field updates
/// </summary>
public class DeviceRpcExecutorService : IDeviceRpcExecutorService
{
    private readonly IThingsBoardConnectorService _tbService;

    public DeviceRpcExecutorService(IThingsBoardConnectorService tbService)
    {
        _tbService = tbService;
    }

    public async Task<RpcExecutionResult> ExecuteParameterUpdateAsync(
        Guid tbDeviceId,
        string parameterName,
        string value,
        CancellationToken cancellationToken = default)
    {
        // Get RPC method mapping
        if (!RpcMethodMapping.TryGetMapping(parameterName, out var config) || config == null)
        {
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"No RPC mapping found for parameter: {parameterName}"
            };
        }

        try
        {
            // Build strongly-typed request with ONLY the changed field
            var mappedValue = config.ValueMapper != null ? config.ValueMapper(value) : value;
            var request = BuildRequestObject(config.RpcKind, config.FieldPath, mappedValue);

            // Convert request object to dictionary for RPC call
            var requestDict = SerializeToDict(request);

            // Send request (device will fill in undefined fields from current state)
            var updateResponse = await _tbService.SendTwoWayRpcAsync(
                deviceId: tbDeviceId,
                method: config.RpcMethod,
                parameters: requestDict,
                timeoutMs: 60000, // 60 seconds - allows time for Modbus operations + network delays
                cancellationToken: cancellationToken
            );

            if (updateResponse == null || !updateResponse.Success)
            {
                return new RpcExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Device did not confirm parameter update",
                    RpcMethod = config.RpcMethod
                };
            }

            return new RpcExecutionResult
            {
                Success = true,
                RpcMethod = config.RpcMethod,
                Response = updateResponse.Response
            };
        }
        catch (Exception ex)
        {
            return new RpcExecutionResult
            {
                Success = false,
                ErrorMessage = $"RPC execution failed: {ex.Message}",
                RpcMethod = config.RpcMethod
            };
        }
    }

    public async Task<Dictionary<string, RpcExecutionResult>> ExecuteParameterUpdatesAsync(
        Guid tbDeviceId,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, RpcExecutionResult>();

        // Group parameters by RPC method to minimize calls
        var parametersByMethod = parameters
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
            try
            {
                // Create empty request object for this RPC kind
                var request = CreateEmptyRequest(group.Key.RpcKind);

                // Populate request with all changed fields
                foreach (var param in group)
                {
                    var mappedValue = param.Mapping!.ValueMapper != null
                        ? param.Mapping.ValueMapper(param.Value)
                        : param.Value;

                    PopulateRequestField(request, param.Mapping.FieldPath, mappedValue);
                }

                // Convert request object to dictionary for RPC call
                var requestDict = SerializeToDict(request);

                // Send request (device will fill in undefined fields)
                var updateResponse = await _tbService.SendTwoWayRpcAsync(
                    deviceId: tbDeviceId,
                    method: group.Key.RpcMethod,
                    parameters: requestDict,
                    timeoutMs: 60000, // 60 seconds - only writes changed registers
                    cancellationToken: cancellationToken
                );

                // Mark results for all parameters in this group
                foreach (var param in group)
                {
                    results[param.ParameterName] = new RpcExecutionResult
                    {
                        Success = updateResponse?.Success ?? false,
                        ErrorMessage = updateResponse?.Success == false ? "Device did not confirm update" : null,
                        RpcMethod = group.Key.RpcMethod,
                        Response = updateResponse?.Response
                    };
                }
            }
            catch (Exception ex)
            {
                // Mark all parameters in this group as failed
                foreach (var param in group)
                {
                    results[param.ParameterName] = new RpcExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"RPC execution failed: {ex.Message}",
                        RpcMethod = group.Key.RpcMethod
                    };
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
                config.EndTimeLocal = value as string;
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
}
