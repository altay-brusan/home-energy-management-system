namespace Hems_Backend.Application.DeviceSettings.Models;

/// <summary>
/// Defines the mapping between parameters and RPC methods
/// </summary>
public class RpcMethodMapping
{
    /// <summary>
    /// RPC method name (e.g., "setSystemWorkModeConfig")
    /// </summary>
    public string RpcMethod { get; set; } = string.Empty;

    /// <summary>
    /// RPC method for reading current value (e.g., "getSystemWorkModeConfig")
    /// </summary>
    public string RpcGetMethod { get; set; } = string.Empty;

    /// <summary>
    /// Field path in the RPC payload (e.g., "inverterMode.solarSellEnabled")
    /// Supports nested paths with dot notation
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// RPC kind/category (e.g., "SystemWorkModeConfigState")
    /// </summary>
    public string RpcKind { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter requires special handling
    /// </summary>
    public bool RequiresCustomMapping { get; set; }

    /// <summary>
    /// Custom mapping function name if RequiresCustomMapping is true
    /// </summary>
    public string? CustomMappingFunction { get; set; }
}

/// <summary>
/// Static configuration for parameter to RPC mappings
/// Based on the RPC definitions provided
/// </summary>
public static class ParameterRpcMappings
{
    public static readonly Dictionary<string, RpcMethodMapping> Mappings = new()
    {
        // ============ Battery Settings - Charge Sources ============
        ["GridChargingEnabled"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "grid.enabled",
            RpcKind = "BatteryChargeSourceConfigState"
        },
        ["GridChargingStartSOC"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "grid.startSocPercent",
            RpcKind = "BatteryChargeSourceConfigState"
        },
        ["GridChargingCurrent"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "grid.chargeCurrentA",
            RpcKind = "BatteryChargeSourceConfigState"
        },
        ["GeneratorChargingEnabled"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.enabled",
            RpcKind = "BatteryChargeSourceConfigState"
        },
        ["GeneratorChargingStartSOC"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.startSocPercent",
            RpcKind = "BatteryChargeSourceConfigState"
        },
        ["GeneratorChargingCurrent"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.chargeCurrentA",
            RpcKind = "BatteryChargeSourceConfigState"
        },
        ["GeneratorMaxRunTime"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.maxRunTimeHours",
            RpcKind = "BatteryChargeSourceConfigState"
        },
        ["GeneratorCoolingTime"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.coolingTimeHours",
            RpcKind = "BatteryChargeSourceConfigState"
        },

        // ============ Battery Settings - Protection Thresholds ============
        ["ShutdownSOC"] = new()
        {
            RpcMethod = "setBatteryThresholdConfig",
            RpcGetMethod = "getBatteryThresholdConfig",
            FieldPath = "shutdownCapacityPercent",
            RpcKind = "BatteryThresholdConfigState"
        },
        ["LowBatteryWarning"] = new()
        {
            RpcMethod = "setBatteryThresholdConfig",
            RpcGetMethod = "getBatteryThresholdConfig",
            FieldPath = "lowBatteryCapacityPercent",
            RpcKind = "BatteryThresholdConfigState"
        },
        ["RestartSOC"] = new()
        {
            RpcMethod = "setBatteryThresholdConfig",
            RpcGetMethod = "getBatteryThresholdConfig",
            FieldPath = "restartCapacityPercent",
            RpcKind = "BatteryThresholdConfigState"
        },

        // ============ System Work Mode - Export Strategy ============
        ["ExportStrategySelection"] = new()
        {
            RpcMethod = "setExportStrategy",
            RpcGetMethod = "getExportStrategy",
            FieldPath = "exportStrategy",
            RpcKind = "ExportStrategyState",
            RequiresCustomMapping = true,
            CustomMappingFunction = "MapExportStrategy"
        },

        // ============ System Work Mode - Energy Priority ============
        ["EnergyPrioritySelection"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "energyPriority.pattern",
            RpcKind = "SystemWorkModeConfigState"
        },

        // ============ System Work Mode - Inverter Mode ============
        ["SolarSell"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.solarSellEnabled",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["MaxSellPower"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.maxSellPowerW",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["ZeroExportLimit"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.zeroExportLimit",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["ForceOffGrid"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.forceOffGrid",
            RpcKind = "SystemWorkModeConfigState"
        },

        // ============ System Work Mode - Grid Peak Shaving ============
        ["GridPeakShavingEnabled"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "gridPeakShaving.enabled",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["GridPeakShavingLimitPower"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "gridPeakShaving.limitPowerW",
            RpcKind = "SystemWorkModeConfigState"
        },

        // ============ Time of Use ============
        ["TimeOfUseEnabled"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.enabled",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["Monday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.monday",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["Tuesday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.tuesday",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["Wednesday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.wednesday",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["Thursday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.thursday",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["Friday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.friday",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["Saturday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.saturday",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["Sunday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.sunday",
            RpcKind = "SystemWorkModeConfigState"
        },

        // ============ Generator & Smart Load - Generator Settings ============
        ["GeneratorCharge"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "generatorChargeEnabled",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["GeneratorGridCharge"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "gridChargeEnabled",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["GeneratorStartSOC"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "startSocPercent",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["GeneratorChargeCurrent"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "chargeCurrentA",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },

        // ============ Generator & Smart Load - Generator Peak Shaving ============
        ["GeneratorPeakShavingEnabled"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "generatorPeakShaving.enabled",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["GeneratorPeakShavingLimitPower"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "generatorPeakShaving.limitPowerW",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },

        // ============ Generator & Smart Load - Smart Load ============
        ["SmartLoadEnabled"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.enabled",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["SmartLoadOffSOC"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.offSocPercent",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["SmartLoadOnSOC"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.onSocPercent",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["SmartLoadPVPowerThreshold"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.pvPowerThresholdW",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["SmartLoadOpenDelay"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.openDelayMinutes",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        },
        ["SmartLoadOnGridAlwaysOn"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.onGridAlwaysOn",
            RpcKind = "GeneratorAndSmartLoadConfigState"
        }
    };

    /// <summary>
    /// Get mapping for a parameter by name
    /// </summary>
    public static RpcMethodMapping? GetMapping(string parameterName)
    {
        return Mappings.TryGetValue(parameterName, out var mapping) ? mapping : null;
    }

    /// <summary>
    /// Check if a parameter has RPC mapping
    /// </summary>
    public static bool HasMapping(string parameterName)
    {
        return Mappings.ContainsKey(parameterName);
    }

    /// <summary>
    /// Get all parameters mapped to a specific RPC method
    /// </summary>
    public static List<string> GetParametersForRpcMethod(string rpcMethod)
    {
        return Mappings
            .Where(m => m.Value.RpcMethod == rpcMethod)
            .Select(m => m.Key)
            .ToList();
    }

    /// <summary>
    /// Get all parameters in a specific RPC kind
    /// </summary>
    public static List<string> GetParametersForRpcKind(string rpcKind)
    {
        return Mappings
            .Where(m => m.Value.RpcKind == rpcKind)
            .Select(m => m.Key)
            .ToList();
    }
}
