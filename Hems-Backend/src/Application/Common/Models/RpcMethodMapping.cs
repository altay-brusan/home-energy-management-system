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
        // ===== BATTERY CHARGE SOURCE CONFIGURATION =====
        // RPC: getBatteryChargeSourceConfig / setBatteryChargeSourceConfig

        ["GridChargingEnabled"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "grid.enabled",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["GridChargingStartSOC"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "grid.startSocPercent",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["GridChargingCurrent"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "grid.chargeCurrentA",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["GeneratorChargingEnabled"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.enabled",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["GeneratorChargingStartSOC"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.startSocPercent",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["GeneratorChargingCurrent"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.chargeCurrentA",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["GeneratorMaxRunTime"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.maxRunTimeHours",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => decimal.Parse(val)
        },
        ["GeneratorCoolingTime"] = new()
        {
            RpcMethod = "setBatteryChargeSourceConfig",
            RpcGetMethod = "getBatteryChargeSourceConfig",
            FieldPath = "generator.coolingTimeHours",
            RpcKind = "BatteryChargeSourceConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== BATTERY THRESHOLD CONFIGURATION =====
        // RPC: getBatteryThresholdConfig / setBatteryThresholdConfig

        ["ShutdownSOC"] = new()
        {
            RpcMethod = "setBatteryThresholdConfig",
            RpcGetMethod = "getBatteryThresholdConfig",
            FieldPath = "shutdownCapacityPercent",
            RpcKind = "BatteryThresholdConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["LowBatteryWarning"] = new()
        {
            RpcMethod = "setBatteryThresholdConfig",
            RpcGetMethod = "getBatteryThresholdConfig",
            FieldPath = "lowBatteryCapacityPercent",
            RpcKind = "BatteryThresholdConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["RestartSOC"] = new()
        {
            RpcMethod = "setBatteryThresholdConfig",
            RpcGetMethod = "getBatteryThresholdConfig",
            FieldPath = "restartCapacityPercent",
            RpcKind = "BatteryThresholdConfigState",
            ValueMapper = val => int.Parse(val)
        },

        // ===== SYSTEM WORK MODE CONFIGURATION =====
        // RPC: getSystemWorkModeConfig / setSystemWorkModeConfig

        // Inverter Mode Settings
        ["ExportStrategySelection"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.exportStrategy",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => val switch
            {
                "SellingFirst" => "selling_first",
                "ZeroExportToLoad" => "zero_export_to_load",
                "ZeroExportToCT" => "zero_export_to_ct",
                _ => val
            }
        },
        ["SolarSell"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.solarSellEnabled",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["MaxSellPower"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.maxSellPowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["ZeroExportLimit"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.zeroExportLimitPowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["ForceOffGrid"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "inverterMode.forceOffGrid",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },

        // Energy Priority
        ["EnergyPrioritySelection"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "energyPriority.pattern",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => val switch
            {
                "BatteryFirst" => "battery_first",
                "LoadFirst" => "load_first",
                _ => val.ToLower().Replace(" ", "_")
            }
        },

        // Grid Peak Shaving
        ["GridPeakShavingEnabled"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "gridPeakShaving.enabled",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["GridPeakShavingLimitPower"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "gridPeakShaving.limitPowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },

        // Time of Use - General Settings
        ["TimeOfUseEnabled"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.enabled",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },

        // Time of Use - Active Days
        ["Monday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.monday",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["Tuesday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.tuesday",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["Wednesday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.wednesday",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["Thursday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.thursday",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["Friday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.friday",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["Saturday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.saturday",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["Sunday"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.daysActive.sunday",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },

        // ===== TIME SLOT 1 =====
        ["TimeSlot1Start"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[0].startTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot1End"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[0].endTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot1GridCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[0].allowGridCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot1GenCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[0].allowGeneratorCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot1MaxDischarge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[0].maxBatteryDischargePowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot1MinBatterySOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[0].batteryThresholdSocPercent",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot1MinBatteryVoltage"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[0].batteryThresholdVoltageV",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== TIME SLOT 2 =====
        ["TimeSlot2Start"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[1].startTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot2End"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[1].endTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot2GridCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[1].allowGridCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot2GenCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[1].allowGeneratorCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot2MaxDischarge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[1].maxBatteryDischargePowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot2MinBatterySOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[1].batteryThresholdSocPercent",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot2MinBatteryVoltage"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[1].batteryThresholdVoltageV",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== TIME SLOT 3 =====
        ["TimeSlot3Start"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[2].startTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot3End"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[2].endTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot3GridCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[2].allowGridCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot3GenCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[2].allowGeneratorCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot3MaxDischarge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[2].maxBatteryDischargePowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot3MinBatterySOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[2].batteryThresholdSocPercent",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot3MinBatteryVoltage"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[2].batteryThresholdVoltageV",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== TIME SLOT 4 =====
        ["TimeSlot4Start"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[3].startTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot4End"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[3].endTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot4GridCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[3].allowGridCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot4GenCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[3].allowGeneratorCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot4MaxDischarge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[3].maxBatteryDischargePowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot4MinBatterySOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[3].batteryThresholdSocPercent",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot4MinBatteryVoltage"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[3].batteryThresholdVoltageV",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== TIME SLOT 5 =====
        ["TimeSlot5Start"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[4].startTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot5End"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[4].endTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot5GridCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[4].allowGridCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot5GenCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[4].allowGeneratorCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot5MaxDischarge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[4].maxBatteryDischargePowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot5MinBatterySOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[4].batteryThresholdSocPercent",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot5MinBatteryVoltage"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[4].batteryThresholdVoltageV",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== TIME SLOT 6 =====
        ["TimeSlot6Start"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[5].startTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot6End"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[5].endTimeLocal",
            RpcKind = "SystemWorkModeConfigState"
        },
        ["TimeSlot6GridCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[5].allowGridCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot6GenCharge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[5].allowGeneratorCharge",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["TimeSlot6MaxDischarge"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[5].maxBatteryDischargePowerW",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot6MinBatterySOC"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[5].batteryThresholdSocPercent",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["TimeSlot6MinBatteryVoltage"] = new()
        {
            RpcMethod = "setSystemWorkModeConfig",
            RpcGetMethod = "getSystemWorkModeConfig",
            FieldPath = "timeOfUse.slots[5].batteryThresholdVoltageV",
            RpcKind = "SystemWorkModeConfigState",
            ValueMapper = val => decimal.Parse(val)
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
