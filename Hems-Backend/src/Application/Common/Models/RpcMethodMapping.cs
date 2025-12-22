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

        // ===== GENERATOR AND SMART LOAD CONFIGURATION =====
        // RPC: getGeneratorAndSmartLoadConfig / setGeneratorAndSmartLoadConfig

        ["GeneratorCharge"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "generatorChargeEnabled",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1"
        },
        ["GeneratorGridCharge"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "gridChargeEnabled",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1"
        },
        ["GeneratorStartSOC"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "startSocPercent",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["GeneratorChargeCurrent"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "chargeCurrentA",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["GeneratorPeakShavingEnabled"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "generatorPeakShaving.enabled",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1"
        },
        ["GeneratorPeakShavingLimitPower"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "generatorPeakShaving.limitPowerW",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["SmartLoadEnabled"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.enabled",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["SmartLoadOffSOC"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.offSocPercent",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["SmartLoadOnSOC"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.onSocPercent",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["SmartLoadPVPowerThreshold"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.pvPowerThresholdW",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["SmartLoadOpenDelay"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.openDelayMinutes",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["SmartLoadOnGridAlwaysOn"] = new()
        {
            RpcMethod = "setGeneratorAndSmartLoadConfig",
            RpcGetMethod = "getGeneratorAndSmartLoadConfig",
            FieldPath = "smartLoad.onGridAlwaysOn",
            RpcKind = "GeneratorAndSmartLoadConfigState",
            ValueMapper = val => val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1"
        },

        // ===== BATTERY MAINTENANCE CONFIGURATION =====
        // RPC: getBatteryMaintenanceConfig / setBatteryMaintenanceConfig

        ["BatteryControlMode"] = new()
        {
            RpcMethod = "setBatteryMaintenanceConfig",
            RpcGetMethod = "getBatteryMaintenanceConfig",
            FieldPath = "batteryControlMode",
            RpcKind = "BatteryMaintenanceConfigState",
            ValueMapper = val => val.ToLower().Replace(" ", "_")
        },
        ["LithiumBatteryWakeUpEnabled"] = new()
        {
            RpcMethod = "setBatteryMaintenanceConfig",
            RpcGetMethod = "getBatteryMaintenanceConfig",
            FieldPath = "wakeUp.channel1Enabled",
            RpcKind = "BatteryMaintenanceConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["BatteryResistance"] = new()
        {
            RpcMethod = "setBatteryMaintenanceConfig",
            RpcGetMethod = "getBatteryMaintenanceConfig",
            FieldPath = "batteryResistanceMilliOhm",
            RpcKind = "BatteryMaintenanceConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["ChargeEfficiency"] = new()
        {
            RpcMethod = "setBatteryMaintenanceConfig",
            RpcGetMethod = "getBatteryMaintenanceConfig",
            FieldPath = "chargeEfficiencyPercent",
            RpcKind = "BatteryMaintenanceConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== GRID CODE CONFIGURATION =====
        // RPC: getGridCodeConfig / setGridCodeConfig

        ["GridStandard"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "gridModeRaw",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => val switch
            {
                "General" => 0,
                "UL1741" => 1,
                "CPUC RULE21" => 2,
                "SRD-UL1741" => 3,
                "CEI 0-21" => 4,
                _ => int.Parse(val)
            }
        },
        ["GridFrequency"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "gridFrequencyHz",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["OutputVoltageLevel"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "outputVoltageLevelCode",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => val switch
            {
                "220V" => 0,
                "230V" => 1,
                "240V" => 2,
                "120V" => 3,
                "133V" => 4,
                _ => int.Parse(val)
            }
        },
        ["GridVoltageHigh"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "normalConnect.voltageHighV",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },
        ["GridVoltageLow"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "normalConnect.voltageLowV",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },
        ["GridFrequencyHigh"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "normalConnect.frequencyHighHz",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },
        ["GridFrequencyLow"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "normalConnect.frequencyLowHz",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },
        ["ReconnectionTime"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "reconnectionTimeSeconds",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["PowerFactorTarget"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "powerFactorTarget",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["ActivePowerRegulation"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "activePowerRegulationPercentPerStep",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },
        ["ReactivePowerRegulation"] = new()
        {
            RpcMethod = "setGridCodeConfig",
            RpcGetMethod = "getGridCodeConfig",
            FieldPath = "reactivePowerRegulationPercentPerStep",
            RpcKind = "GridCodeConfigState",
            ValueMapper = val => decimal.Parse(val)
        },

        // ===== INVERTER ADVANCED FUNCTION CONFIGURATION =====
        // RPC: getInverterAdvancedFunctionConfig / setInverterAdvancedFunctionConfig

        ["ArcFaultDetectionEnabled"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "arcFault.enabled",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["IslandProtectionEnabled"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "islandProtectionEnabled",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["ParallelEnabled"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "parallel.enabled",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["ParallelRole"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "parallel.role",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => val.ToLower()
        },
        ["ParallelPhase"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "parallel.phase",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => val.ToUpper()
        },
        ["ModbusAddress"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "parallel.modbusAddress",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["ExternalCTRatio"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "externalMeter.ctRatio",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["ExternalMeterCTRatio"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "externalMeter.meterCtRatio",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => int.Parse(val)
        },
        ["LithiumBatteryLossFaultEnabled"] = new()
        {
            RpcMethod = "setInverterAdvancedFunctionConfig",
            RpcGetMethod = "getInverterAdvancedFunctionConfig",
            FieldPath = "lithiumBatteryLossFaultEnabled",
            RpcKind = "InverterAdvancedFunctionConfigState",
            ValueMapper = val => bool.Parse(val)
        },

        // ===== EXTERNAL RELAY CONFIGURATION =====
        // RPC: getExternalRelayConfig / setExternalRelayConfig

        ["ExternalRelayFeatureEnabled"] = new()
        {
            RpcMethod = "setExternalRelayConfig",
            RpcGetMethod = "getExternalRelayConfig",
            FieldPath = "externalRelayFeatureEnabled",
            RpcKind = "ExternalRelayConfigState",
            ValueMapper = val => bool.Parse(val)
        },
        ["RelayBitmap"] = new()
        {
            RpcMethod = "setExternalRelayConfig",
            RpcGetMethod = "getExternalRelayConfig",
            FieldPath = "relayBitmapRaw",
            RpcKind = "ExternalRelayConfigState",
            ValueMapper = val => int.Parse(val)
        },

        // ===== GENERATOR SMART LOAD OVERRIDE =====
        // RPC: getGeneratorSmartLoadOverride / setGeneratorSmartLoadOverride

        ["ForceSmartLoadOn"] = new()
        {
            RpcMethod = "setGeneratorSmartLoadOverride",
            RpcGetMethod = "getGeneratorSmartLoadOverride",
            FieldPath = "forceSmartLoadOn",
            RpcKind = "GeneratorSmartLoadOverride",
            ValueMapper = val => bool.Parse(val)
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
