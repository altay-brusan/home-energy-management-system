namespace Hems_Backend.Application.DeviceSettings.Models;

/// <summary>
/// Predefined device operating modes with preset parameter configurations
/// Each mode represents a common use case with optimized settings
/// </summary>
public enum DeviceMode
{
    /// <summary>
    /// Balanced Hybrid Mode - Balanced approach between grid, solar, and battery usage
    /// </summary>
    BalancedHybrid = 1,

    /// <summary>
    /// Zero Export Mode - Prevent exporting power to the grid while maintaining load coverage
    /// </summary>
    ZeroExport = 2,

    /// <summary>
    /// Maximum Saving Mode - Maximize energy savings by prioritizing battery usage and minimizing grid consumption
    /// </summary>
    MaximumSaving = 3,

    /// <summary>
    /// Custom Mode - User-defined configuration
    /// </summary>
    Custom = 99
}

/// <summary>
/// Predefined parameter configurations for each device mode
/// Based on comprehensive mode configuration booklet
/// </summary>
public static class DeviceModeConfigurations
{
    /// <summary>
    /// Get parameter configuration for a specific device mode
    /// </summary>
    public static Dictionary<string, string>? GetConfiguration(DeviceMode mode)
    {
        return Configurations.TryGetValue(mode, out var config) ? config : null;
    }

    /// <summary>
    /// Mode configurations mapping
    /// Key: DeviceMode enum
    /// Value: Dictionary of parameter names to their values
    /// </summary>
    public static readonly Dictionary<DeviceMode, Dictionary<string, string>> Configurations = new()
    {
        [DeviceMode.BalancedHybrid] = new()
        {
            // Grid and Generator Charging
            ["GridChargingEnabled"] = "true",
            ["GridChargingStartSOC"] = "30",
            ["GridChargingCurrent"] = "20",
            ["GeneratorChargingEnabled"] = "false",
            ["GeneratorChargingStartSOC"] = "30",
            ["GeneratorChargingCurrent"] = "25",
            ["GeneratorMaxRunTime"] = "12",
            ["GeneratorCoolingTime"] = "8",

            // Battery Protection
            ["ShutdownSOC"] = "10",
            ["LowBatteryWarning"] = "20",
            ["RestartSOC"] = "50",

            // Export Strategy
            ["ExportStrategySelection"] = "ZeroExportToLoad",
            ["EnergyPrioritySelection"] = "LoadFirst",
            ["SolarSell"] = "false",
            ["MaxSellPower"] = "100",
            ["ZeroExportLimit"] = "20",
            ["ForceOffGrid"] = "false",

            // Grid Peak Shaving
            ["GridPeakShavingEnabled"] = "false",
            ["GridPeakShavingLimitPower"] = "2000",

            // Time of Use
            ["TimeOfUseEnabled"] = "true",
            ["Monday"] = "true",
            ["Tuesday"] = "true",
            ["Wednesday"] = "true",
            ["Thursday"] = "true",
            ["Friday"] = "true",
            ["Saturday"] = "true",
            ["Sunday"] = "true",

            // Time Slot 1 (01:00)
            ["TimeSlot1Start"] = "01:00",
            ["TimeSlot1GridCharge"] = "true",
            ["TimeSlot1GenCharge"] = "false",
            ["TimeSlot1MaxDischarge"] = "2000",
            ["TimeSlot1MinBatterySOC"] = "100",
            ["TimeSlot1MinBatteryVoltage"] = "48",

            // Time Slot 2 (05:00)
            ["TimeSlot2Start"] = "05:00",
            ["TimeSlot2GridCharge"] = "true",
            ["TimeSlot2GenCharge"] = "false",
            ["TimeSlot2MaxDischarge"] = "2000",
            ["TimeSlot2MinBatterySOC"] = "80",
            ["TimeSlot2MinBatteryVoltage"] = "48",

            // Time Slot 3 (09:00)
            ["TimeSlot3Start"] = "09:00",
            ["TimeSlot3GridCharge"] = "true",
            ["TimeSlot3GenCharge"] = "false",
            ["TimeSlot3MaxDischarge"] = "8000",
            ["TimeSlot3MinBatterySOC"] = "70",
            ["TimeSlot3MinBatteryVoltage"] = "48",

            // Time Slot 4 (13:00)
            ["TimeSlot4Start"] = "13:00",
            ["TimeSlot4GridCharge"] = "true",
            ["TimeSlot4GenCharge"] = "false",
            ["TimeSlot4MaxDischarge"] = "6000",
            ["TimeSlot4MinBatterySOC"] = "100",

            // Time Slot 5 (17:00)
            ["TimeSlot5Start"] = "17:00",
            ["TimeSlot5GridCharge"] = "false",
            ["TimeSlot5GenCharge"] = "false",
            ["TimeSlot5MaxDischarge"] = "10000",
            ["TimeSlot5MinBatterySOC"] = "30",
            ["TimeSlot5MinBatteryVoltage"] = "48",

            // Time Slot 6 (21:00)
            ["TimeSlot6Start"] = "21:00",
            ["TimeSlot6GridCharge"] = "false",
            ["TimeSlot6GenCharge"] = "false",
            ["TimeSlot6MaxDischarge"] = "10000",
            ["TimeSlot6MinBatterySOC"] = "30",
            ["TimeSlot6MinBatteryVoltage"] = "48",

            // Generator and Smart Load
            ["GeneratorCharge"] = "false",
            ["GeneratorGridCharge"] = "false",
            ["GeneratorStartSOC"] = "30",
            ["GeneratorChargeCurrent"] = "15",
            ["GeneratorPeakShavingEnabled"] = "false",
            ["GeneratorPeakShavingLimitPower"] = "10000",
            ["SmartLoadEnabled"] = "false",
            ["SmartLoadOffSOC"] = "30",
            ["SmartLoadOnSOC"] = "95",
            ["SmartLoadPVPowerThreshold"] = "1000",
            ["SmartLoadOnGridAlwaysOn"] = "false",
        },

        [DeviceMode.ZeroExport] = new()
        {
            // Grid and Generator Charging
            ["GridChargingEnabled"] = "true",
            ["GridChargingStartSOC"] = "30",
            ["GridChargingCurrent"] = "20",
            ["GeneratorChargingEnabled"] = "false",
            ["GeneratorChargingStartSOC"] = "30",
            ["GeneratorChargingCurrent"] = "25",
            ["GeneratorMaxRunTime"] = "12",
            ["GeneratorCoolingTime"] = "8",

            // Battery Protection
            ["ShutdownSOC"] = "10",
            ["LowBatteryWarning"] = "20",
            ["RestartSOC"] = "50",

            // Export Strategy
            ["ExportStrategySelection"] = "ZeroExportToLoad",
            ["EnergyPrioritySelection"] = "LoadFirst",
            ["SolarSell"] = "false",
            ["MaxSellPower"] = "100",
            ["ZeroExportLimit"] = "20",
            ["ForceOffGrid"] = "false",

            // Grid Peak Shaving
            ["GridPeakShavingEnabled"] = "false",
            ["GridPeakShavingLimitPower"] = "2000",

            // Time of Use
            ["TimeOfUseEnabled"] = "true",
            ["Monday"] = "true",
            ["Tuesday"] = "true",
            ["Wednesday"] = "true",
            ["Thursday"] = "true",
            ["Friday"] = "true",
            ["Saturday"] = "true",
            ["Sunday"] = "true",

            // Time Slot 1 (01:00)
            ["TimeSlot1Start"] = "01:00",
            ["TimeSlot1GridCharge"] = "true",
            ["TimeSlot1GenCharge"] = "false",
            ["TimeSlot1MaxDischarge"] = "2000",
            ["TimeSlot1MinBatterySOC"] = "100",
            ["TimeSlot1MinBatteryVoltage"] = "48",

            // Time Slot 2 (05:00)
            ["TimeSlot2Start"] = "05:00",
            ["TimeSlot2GridCharge"] = "true",
            ["TimeSlot2GenCharge"] = "false",
            ["TimeSlot2MaxDischarge"] = "3000",
            ["TimeSlot2MinBatterySOC"] = "90",
            ["TimeSlot2MinBatteryVoltage"] = "48",

            // Time Slot 3 (09:00)
            ["TimeSlot3Start"] = "09:00",
            ["TimeSlot3GridCharge"] = "true",
            ["TimeSlot3GenCharge"] = "false",
            ["TimeSlot3MaxDischarge"] = "9000",
            ["TimeSlot3MinBatterySOC"] = "60",
            ["TimeSlot3MinBatteryVoltage"] = "48",

            // Time Slot 4 (13:00)
            ["TimeSlot4Start"] = "13:00",
            ["TimeSlot4GridCharge"] = "true",
            ["TimeSlot4GenCharge"] = "false",
            ["TimeSlot4MaxDischarge"] = "6000",
            ["TimeSlot4MinBatterySOC"] = "100",

            // Time Slot 5 (17:00)
            ["TimeSlot5Start"] = "17:00",
            ["TimeSlot5GridCharge"] = "false",
            ["TimeSlot5GenCharge"] = "false",
            ["TimeSlot5MaxDischarge"] = "10000",
            ["TimeSlot5MinBatterySOC"] = "30",
            ["TimeSlot5MinBatteryVoltage"] = "48",

            // Time Slot 6 (21:00)
            ["TimeSlot6Start"] = "21:00",
            ["TimeSlot6GridCharge"] = "false",
            ["TimeSlot6GenCharge"] = "false",
            ["TimeSlot6MaxDischarge"] = "10000",
            ["TimeSlot6MinBatterySOC"] = "30",
            ["TimeSlot6MinBatteryVoltage"] = "48",

            // Generator and Smart Load
            ["GeneratorCharge"] = "false",
            ["GeneratorGridCharge"] = "false",
            ["GeneratorStartSOC"] = "30",
            ["GeneratorChargeCurrent"] = "15",
            ["GeneratorPeakShavingEnabled"] = "false",
            ["GeneratorPeakShavingLimitPower"] = "10000",
            ["SmartLoadEnabled"] = "false",
            ["SmartLoadOffSOC"] = "30",
            ["SmartLoadOnSOC"] = "95",
            ["SmartLoadPVPowerThreshold"] = "1000",
            ["SmartLoadOnGridAlwaysOn"] = "false",
        },

        [DeviceMode.MaximumSaving] = new()
        {
            // Grid and Generator Charging
            ["GridChargingEnabled"] = "true",
            ["GridChargingStartSOC"] = "20",
            ["GridChargingCurrent"] = "20",
            ["GeneratorChargingEnabled"] = "false",
            ["GeneratorChargingStartSOC"] = "30",
            ["GeneratorChargingCurrent"] = "25",
            ["GeneratorMaxRunTime"] = "12",
            ["GeneratorCoolingTime"] = "8",

            // Battery Protection
            ["ShutdownSOC"] = "10",
            ["LowBatteryWarning"] = "20",
            ["RestartSOC"] = "50",

            // Export Strategy
            ["ExportStrategySelection"] = "ZeroExportToLoad",
            ["EnergyPrioritySelection"] = "BatteryFirst",
            ["SolarSell"] = "false",
            ["MaxSellPower"] = "100",
            ["ZeroExportLimit"] = "20",
            ["ForceOffGrid"] = "false",

            // Grid Peak Shaving
            ["GridPeakShavingEnabled"] = "true",
            ["GridPeakShavingLimitPower"] = "3000",

            // Time of Use
            ["TimeOfUseEnabled"] = "true",
            ["Monday"] = "true",
            ["Tuesday"] = "true",
            ["Wednesday"] = "true",
            ["Thursday"] = "true",
            ["Friday"] = "true",
            ["Saturday"] = "true",
            ["Sunday"] = "true",

            // Time Slot 1 (01:00)
            ["TimeSlot1Start"] = "01:00",
            ["TimeSlot1GridCharge"] = "true",
            ["TimeSlot1GenCharge"] = "false",
            ["TimeSlot1MaxDischarge"] = "2000",
            ["TimeSlot1MinBatterySOC"] = "100",
            ["TimeSlot1MinBatteryVoltage"] = "48",

            // Time Slot 2 (05:00)
            ["TimeSlot2Start"] = "05:00",
            ["TimeSlot2GridCharge"] = "true",
            ["TimeSlot2GenCharge"] = "false",
            ["TimeSlot2MaxDischarge"] = "3000",
            ["TimeSlot2MinBatterySOC"] = "80",
            ["TimeSlot2MinBatteryVoltage"] = "48",

            // Time Slot 3 (09:00)
            ["TimeSlot3Start"] = "09:00",
            ["TimeSlot3GridCharge"] = "false",
            ["TimeSlot3GenCharge"] = "false",
            ["TimeSlot3MaxDischarge"] = "10000",
            ["TimeSlot3MinBatterySOC"] = "50",
            ["TimeSlot3MinBatteryVoltage"] = "48",

            // Time Slot 4 (13:00)
            ["TimeSlot4Start"] = "13:00",
            ["TimeSlot4GridCharge"] = "false",
            ["TimeSlot4GenCharge"] = "false",
            ["TimeSlot4MaxDischarge"] = "6000",
            ["TimeSlot4MinBatterySOC"] = "100",

            // Time Slot 5 (17:00)
            ["TimeSlot5Start"] = "17:00",
            ["TimeSlot5GridCharge"] = "false",
            ["TimeSlot5GenCharge"] = "false",
            ["TimeSlot5MaxDischarge"] = "10000",
            ["TimeSlot5MinBatterySOC"] = "20",
            ["TimeSlot5MinBatteryVoltage"] = "48",

            // Time Slot 6 (21:00)
            ["TimeSlot6Start"] = "21:00",
            ["TimeSlot6GridCharge"] = "false",
            ["TimeSlot6GenCharge"] = "false",
            ["TimeSlot6MaxDischarge"] = "10000",
            ["TimeSlot6MinBatterySOC"] = "20",
            ["TimeSlot6MinBatteryVoltage"] = "48",

            // Generator and Smart Load
            ["GeneratorCharge"] = "false",
            ["GeneratorGridCharge"] = "false",
            ["GeneratorStartSOC"] = "20",
            ["GeneratorChargeCurrent"] = "15",
            ["GeneratorPeakShavingEnabled"] = "false",
            ["GeneratorPeakShavingLimitPower"] = "10000",
            ["SmartLoadEnabled"] = "false",
            ["SmartLoadOffSOC"] = "30",
            ["SmartLoadOnSOC"] = "95",
            ["SmartLoadPVPowerThreshold"] = "1000",
            ["SmartLoadOnGridAlwaysOn"] = "false",
        },
    };

    /// <summary>
    /// Get all available modes with their descriptions
    /// </summary>
    public static Dictionary<DeviceMode, string> GetAllModesWithDescriptions()
    {
        return new Dictionary<DeviceMode, string>
        {
            [DeviceMode.BalancedHybrid] = "Balanced approach between grid, solar, and battery usage with load-first priority",
            [DeviceMode.ZeroExport] = "Prevent exporting power to the grid while maintaining load coverage",
            [DeviceMode.MaximumSaving] = "Maximize energy savings by prioritizing battery usage (BatteryFirst) and minimizing grid consumption with peak shaving",
            [DeviceMode.Custom] = "User-defined custom configuration"
        };
    }
}
