namespace Hems_Backend.Application.Common.Models;

/// <summary>
/// Static mapping between parameter names and their RPC methods
/// Maps application parameter names to device RPC method names and field paths
/// </summary>
public static class RpcMethodMapping
{
    /// <summary>
    /// Configuration for a parameter's RPC mapping
    /// </summary>
    public record RpcMethodConfig
    {
        /// <summary>
        /// RPC method name for setting this parameter
        /// </summary>
        public required string RpcMethod { get; init; }

        /// <summary>
        /// RPC method name for getting this parameter's current value
        /// </summary>
        public required string RpcGetMethod { get; init; }

        /// <summary>
        /// Field path within the RPC payload (e.g., "inverterMode.solarSellEnabled")
        /// </summary>
        public required string FieldPath { get; init; }

        /// <summary>
        /// RPC kind/type identifier (e.g., "SystemWorkModeConfigState")
        /// </summary>
        public required string RpcKind { get; init; }

        /// <summary>
        /// Custom value mapper for complex types (optional)
        /// </summary>
        public Func<string, object>? ValueMapper { get; init; }
    }

    /// <summary>
    /// Parameter to RPC method mapping dictionary
    /// </summary>
    public static readonly Dictionary<string, RpcMethodConfig> ParameterMappings = new()
    {
        // ===== INVERTER MODE PARAMETERS =====
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
            FieldPath = "inverterMode.maxSellPower",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["ExportStrategySelection"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.exportStrategySelection",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => val switch
            {
                "SellingFirst" => 0,
                "ZeroExportToLoad" => 1,
                "ZeroExportToCT" => 2,
                _ => int.Parse(val)
            }
        },

        // ===== CHARGE SOURCE PARAMETERS =====
        ["GridChargingEnabled"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "chargeSource.gridChargingEnabled",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["SolarChargingEnabled"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "chargeSource.solarChargingEnabled",
            RpcKind = "SystemWorkModeConfigState"
        },

        // ===== PROTECTION THRESHOLDS =====
        ["ShutdownSOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "protectionThresholds.shutdownSOC",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["LowSOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "protectionThresholds.lowSOC",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },

        // ===== BATTERY SETTINGS =====
        ["ChargeCurrentLimit"] = new()
        {
            RpcMethod = "setBatteryConfig",
            RpcGetMethod = "getBatteryConfig",
            FieldPath = "chargeCurrentLimit",
            RpcKind = "BatteryConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["DischargeCurrentLimit"] = new()
        {
            RpcMethod = "setBatteryConfig",
            RpcGetMethod = "getBatteryConfig",
            FieldPath = "dischargeCurrentLimit",
            RpcKind = "BatteryConfigState",
            ValueMapper = val => int.Parse(val)
        },

        // ===== TIME OF USE SETTINGS =====
        ["TimeOfUseEnabled"] = new()
        {
            RpcMethod = "setTimeOfUseConfig",
            RpcGetMethod = "getTimeOfUseConfig",
            FieldPath = "enabled",
            RpcKind = "TimeOfUseConfigState"
        },
        ["PeakStartTime"] = new()
        {
            RpcMethod = "setTimeOfUseConfig",
            RpcGetMethod = "getTimeOfUseConfig",
            FieldPath = "peakStartTime",
            RpcKind = "TimeOfUseConfigState"
        },
        ["PeakEndTime"] = new()
        {
            RpcMethod = "setTimeOfUseConfig",
            RpcGetMethod = "getTimeOfUseConfig",
            FieldPath = "peakEndTime",
            RpcKind = "TimeOfUseConfigState"
        },
        ["OffPeakStartTime"] = new()
        {
            RpcMethod = "setTimeOfUseConfig",
            RpcGetMethod = "getTimeOfUseConfig",
            FieldPath = "offPeakStartTime",
            RpcKind = "TimeOfUseConfigState"
        },
        ["OffPeakEndTime"] = new()
        {
            RpcMethod = "setTimeOfUseConfig",
            RpcGetMethod = "getTimeOfUseConfig",
            FieldPath = "offPeakEndTime",
            RpcKind = "TimeOfUseConfigState"
        },
    };

    /// <summary>
    /// Try to get RPC method configuration for a parameter
    /// </summary>
    public static bool TryGetMapping(string parameterName, out RpcMethodConfig? config)
    {
        return ParameterMappings.TryGetValue(parameterName, out config);
    }
}
