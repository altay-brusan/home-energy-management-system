namespace Hems_Backend.Application.DeviceSettings.Models;

/// <summary>
/// Predefined device operating modes with preset parameter configurations
/// Each mode represents a common use case with optimized settings
/// </summary>
public enum DeviceMode
{
    /// <summary>
    /// Energy Save Mode - Maximize battery life, minimize grid usage
    /// </summary>
    EnergySave = 1,

    /// <summary>
    /// Max Performance Mode - Maximize power output and responsiveness
    /// </summary>
    MaxPerformance = 2,

    /// <summary>
    /// Self Consumption Mode - Prioritize using solar for home, store excess
    /// </summary>
    SelfConsumption = 3,

    /// <summary>
    /// Backup Mode - Keep battery charged for emergencies
    /// </summary>
    Backup = 4,

    /// <summary>
    /// Time of Use Mode - Optimize based on utility rates
    /// </summary>
    TimeOfUse = 5,

    /// <summary>
    /// Off Grid Mode - No grid connection, solar and battery only
    /// </summary>
    OffGrid = 6,

    /// <summary>
    /// Grid Selling Mode - Maximize revenue by selling to grid
    /// </summary>
    GridSelling = 7,

    /// <summary>
    /// Custom Mode - User-defined configuration
    /// </summary>
    Custom = 99
}

/// <summary>
/// Predefined parameter configurations for each device mode
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
        [DeviceMode.EnergySave] = new()
        {
            ["GridChargingEnabled"] = "false",
            ["SolarChargingEnabled"] = "true",
            ["SolarSell"] = "false",
            ["ExportStrategySelection"] = "ZeroExportToCT",
            ["ShutdownSOC"] = "15",
            ["LowSOC"] = "20",
        },

        [DeviceMode.MaxPerformance] = new()
        {
            ["GridChargingEnabled"] = "true",
            ["SolarChargingEnabled"] = "true",
            ["SolarSell"] = "true",
            ["MaxSellPower"] = "12000",
            ["ExportStrategySelection"] = "SellingFirst",
            ["ShutdownSOC"] = "10",
            ["LowSOC"] = "15",
        },

        [DeviceMode.SelfConsumption] = new()
        {
            ["GridChargingEnabled"] = "false",
            ["SolarChargingEnabled"] = "true",
            ["SolarSell"] = "false",
            ["ExportStrategySelection"] = "ZeroExportToLoad",
            ["ShutdownSOC"] = "20",
            ["LowSOC"] = "30",
        },

        [DeviceMode.Backup] = new()
        {
            ["GridChargingEnabled"] = "true",
            ["SolarChargingEnabled"] = "true",
            ["SolarSell"] = "false",
            ["ExportStrategySelection"] = "ZeroExportToCT",
            ["ShutdownSOC"] = "30",
            ["LowSOC"] = "50",
        },

        [DeviceMode.TimeOfUse] = new()
        {
            ["GridChargingEnabled"] = "true",
            ["SolarChargingEnabled"] = "true",
            ["SolarSell"] = "true",
            ["TimeOfUseEnabled"] = "true",
            ["ExportStrategySelection"] = "SellingFirst",
            ["ShutdownSOC"] = "15",
            ["LowSOC"] = "25",
        },

        [DeviceMode.OffGrid] = new()
        {
            ["GridChargingEnabled"] = "false",
            ["SolarChargingEnabled"] = "true",
            ["SolarSell"] = "false",
            ["ExportStrategySelection"] = "ZeroExportToCT",
            ["ShutdownSOC"] = "25",
            ["LowSOC"] = "35",
        },

        [DeviceMode.GridSelling] = new()
        {
            ["GridChargingEnabled"] = "true",
            ["SolarChargingEnabled"] = "true",
            ["SolarSell"] = "true",
            ["MaxSellPower"] = "12000",
            ["ExportStrategySelection"] = "SellingFirst",
            ["ShutdownSOC"] = "10",
            ["LowSOC"] = "20",
        },
    };

    /// <summary>
    /// Get all available modes with their descriptions
    /// </summary>
    public static Dictionary<DeviceMode, string> GetAllModesWithDescriptions()
    {
        return new Dictionary<DeviceMode, string>
        {
            [DeviceMode.EnergySave] = "Maximize battery life and minimize grid usage",
            [DeviceMode.MaxPerformance] = "Maximize power output and system responsiveness",
            [DeviceMode.SelfConsumption] = "Prioritize using solar energy for home consumption",
            [DeviceMode.Backup] = "Keep battery charged for emergency power backup",
            [DeviceMode.TimeOfUse] = "Optimize based on utility rate schedules",
            [DeviceMode.OffGrid] = "Operate independently without grid connection",
            [DeviceMode.GridSelling] = "Maximize revenue by selling excess power to grid",
            [DeviceMode.Custom] = "User-defined custom configuration"
        };
    }
}
