namespace Hems_Backend.Application.DeviceSettings.Models;

/// <summary>
/// Predefined device operating modes
/// </summary>
public enum DeviceMode
{
    /// <summary>
    /// Energy saving mode - prioritizes battery preservation
    /// </summary>
    EnergySave = 1,

    /// <summary>
    /// Maximum performance mode - prioritizes power output
    /// </summary>
    MaxPerformance = 2,

    /// <summary>
    /// Self-consumption mode - use solar power for own consumption
    /// </summary>
    SelfConsumption = 3,

    /// <summary>
    /// Backup mode - prioritizes keeping battery charged for backup
    /// </summary>
    Backup = 4,

    /// <summary>
    /// Time of use optimization mode - optimizes based on electricity rates
    /// </summary>
    TimeOfUse = 5,

    /// <summary>
    /// Off-grid mode - operates independently from grid
    /// </summary>
    OffGrid = 6,

    /// <summary>
    /// Grid selling mode - prioritizes selling excess solar to grid
    /// </summary>
    GridSelling = 7,

    /// <summary>
    /// Custom mode - user-defined settings
    /// </summary>
    Custom = 99
}

/// <summary>
/// Configuration for each device mode
/// Maps mode to parameter values
/// </summary>
public static class DeviceModeConfigurations
{
    public static readonly Dictionary<DeviceMode, Dictionary<string, string>> ModeSettings = new()
    {
        [DeviceMode.EnergySave] = new()
        {
            // Battery Settings
            ["GridChargingEnabled"] = "false",
            ["GeneratorChargingEnabled"] = "false",
            ["ShutdownSOC"] = "15",
            ["LowBatteryWarning"] = "30",
            ["RestartSOC"] = "40",

            // System Work Mode
            ["ExportStrategySelection"] = "ZeroExportToCT",
            ["EnergyPrioritySelection"] = "BatteryFirst",
            ["SolarSell"] = "false",
            ["ForceOffGrid"] = "false",
            ["GridPeakShavingEnabled"] = "true",
            ["GridPeakShavingLimitPower"] = "5000",

            // Generator & Smart Load
            ["SmartLoadEnabled"] = "true",
            ["SmartLoadOffSOC"] = "95",
            ["SmartLoadOnSOC"] = "100",
            ["GeneratorPeakShavingEnabled"] = "true"
        },

        [DeviceMode.MaxPerformance] = new()
        {
            // Battery Settings
            ["GridChargingEnabled"] = "true",
            ["GeneratorChargingEnabled"] = "true",
            ["ShutdownSOC"] = "10",
            ["LowBatteryWarning"] = "20",
            ["RestartSOC"] = "50",

            // System Work Mode
            ["ExportStrategySelection"] = "SellingFirst",
            ["EnergyPrioritySelection"] = "LoadFirst",
            ["SolarSell"] = "true",
            ["MaxSellPower"] = "12000",
            ["ForceOffGrid"] = "false",
            ["GridPeakShavingEnabled"] = "false",

            // Generator & Smart Load
            ["SmartLoadEnabled"] = "false",
            ["GeneratorPeakShavingEnabled"] = "false"
        },

        [DeviceMode.SelfConsumption] = new()
        {
            // Battery Settings
            ["GridChargingEnabled"] = "false",
            ["GeneratorChargingEnabled"] = "false",
            ["ShutdownSOC"] = "20",
            ["LowBatteryWarning"] = "35",
            ["RestartSOC"] = "50",

            // System Work Mode
            ["ExportStrategySelection"] = "ZeroExportToCT",
            ["EnergyPrioritySelection"] = "BatteryFirst",
            ["SolarSell"] = "false",
            ["ForceOffGrid"] = "false",
            ["GridPeakShavingEnabled"] = "false",

            // Generator & Smart Load
            ["SmartLoadEnabled"] = "true",
            ["SmartLoadOffSOC"] = "90",
            ["SmartLoadOnSOC"] = "95",
            ["GeneratorPeakShavingEnabled"] = "false"
        },

        [DeviceMode.Backup] = new()
        {
            // Battery Settings
            ["GridChargingEnabled"] = "true",
            ["GridChargingStartSOC"] = "80",
            ["GeneratorChargingEnabled"] = "true",
            ["GeneratorChargingStartSOC"] = "70",
            ["ShutdownSOC"] = "30",
            ["LowBatteryWarning"] = "50",
            ["RestartSOC"] = "60",

            // System Work Mode
            ["ExportStrategySelection"] = "ZeroExportToLoad",
            ["EnergyPrioritySelection"] = "BatteryFirst",
            ["SolarSell"] = "false",
            ["ForceOffGrid"] = "false",
            ["GridPeakShavingEnabled"] = "true",
            ["GridPeakShavingLimitPower"] = "3000",

            // Generator & Smart Load
            ["SmartLoadEnabled"] = "false",
            ["GeneratorPeakShavingEnabled"] = "true",
            ["GeneratorPeakShavingLimitPower"] = "3000"
        },

        [DeviceMode.TimeOfUse] = new()
        {
            // Battery Settings
            ["GridChargingEnabled"] = "true",
            ["GridChargingStartSOC"] = "50",
            ["GeneratorChargingEnabled"] = "false",
            ["ShutdownSOC"] = "20",
            ["LowBatteryWarning"] = "35",
            ["RestartSOC"] = "50",

            // System Work Mode
            ["ExportStrategySelection"] = "ZeroExportToLoad",
            ["EnergyPrioritySelection"] = "BatteryFirst",
            ["SolarSell"] = "false",
            ["ForceOffGrid"] = "false",
            ["GridPeakShavingEnabled"] = "true",
            ["GridPeakShavingLimitPower"] = "6000",

            // Time of Use
            ["TimeOfUseEnabled"] = "true",
            ["Monday"] = "true",
            ["Tuesday"] = "true",
            ["Wednesday"] = "true",
            ["Thursday"] = "true",
            ["Friday"] = "true",
            ["Saturday"] = "true",
            ["Sunday"] = "true",

            // Generator & Smart Load
            ["SmartLoadEnabled"] = "false",
            ["GeneratorPeakShavingEnabled"] = "false"
        },

        [DeviceMode.OffGrid] = new()
        {
            // Battery Settings
            ["GridChargingEnabled"] = "false",
            ["GeneratorChargingEnabled"] = "true",
            ["GeneratorChargingStartSOC"] = "30",
            ["ShutdownSOC"] = "20",
            ["LowBatteryWarning"] = "35",
            ["RestartSOC"] = "50",

            // System Work Mode
            ["ExportStrategySelection"] = "ZeroExportToLoad",
            ["EnergyPrioritySelection"] = "BatteryFirst",
            ["SolarSell"] = "false",
            ["ForceOffGrid"] = "true",
            ["GridPeakShavingEnabled"] = "false",

            // Generator & Smart Load
            ["GeneratorCharge"] = "true",
            ["GeneratorStartSOC"] = "30",
            ["SmartLoadEnabled"] = "true",
            ["SmartLoadOffSOC"] = "85",
            ["SmartLoadOnSOC"] = "95",
            ["GeneratorPeakShavingEnabled"] = "true",
            ["GeneratorPeakShavingLimitPower"] = "8000"
        },

        [DeviceMode.GridSelling] = new()
        {
            // Battery Settings
            ["GridChargingEnabled"] = "false",
            ["GeneratorChargingEnabled"] = "false",
            ["ShutdownSOC"] = "15",
            ["LowBatteryWarning"] = "25",
            ["RestartSOC"] = "40",

            // System Work Mode
            ["ExportStrategySelection"] = "SellingFirst",
            ["EnergyPrioritySelection"] = "LoadFirst",
            ["SolarSell"] = "true",
            ["MaxSellPower"] = "12000",
            ["ForceOffGrid"] = "false",
            ["GridPeakShavingEnabled"] = "false",

            // Generator & Smart Load
            ["SmartLoadEnabled"] = "false",
            ["GeneratorPeakShavingEnabled"] = "false"
        }
    };

    /// <summary>
    /// Get configuration for a specific mode
    /// </summary>
    public static Dictionary<string, string>? GetModeConfiguration(DeviceMode mode)
    {
        return ModeSettings.TryGetValue(mode, out var config) ? config : null;
    }

    /// <summary>
    /// Get all available modes with descriptions
    /// </summary>
    public static List<DeviceModeInfo> GetAvailableModes()
    {
        return new List<DeviceModeInfo>
        {
            new()
            {
                Mode = DeviceMode.EnergySave,
                Name = "Energy Save",
                Description = "Prioritizes battery preservation and minimizes grid consumption"
            },
            new()
            {
                Mode = DeviceMode.MaxPerformance,
                Name = "Max Performance",
                Description = "Prioritizes power output and allows grid charging"
            },
            new()
            {
                Mode = DeviceMode.SelfConsumption,
                Name = "Self Consumption",
                Description = "Maximizes use of solar power for own consumption"
            },
            new()
            {
                Mode = DeviceMode.Backup,
                Name = "Backup",
                Description = "Keeps battery charged for backup power during outages"
            },
            new()
            {
                Mode = DeviceMode.TimeOfUse,
                Name = "Time of Use",
                Description = "Optimizes based on electricity rate schedules"
            },
            new()
            {
                Mode = DeviceMode.OffGrid,
                Name = "Off-Grid",
                Description = "Operates independently from the grid"
            },
            new()
            {
                Mode = DeviceMode.GridSelling,
                Name = "Grid Selling",
                Description = "Prioritizes selling excess solar power to the grid"
            },
            new()
            {
                Mode = DeviceMode.Custom,
                Name = "Custom",
                Description = "User-defined custom settings"
            }
        };
    }
}

public class DeviceModeInfo
{
    public DeviceMode Mode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
