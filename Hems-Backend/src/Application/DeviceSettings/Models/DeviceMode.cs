namespace Hems_Backend.Application.DeviceSettings.Models;

/// <summary>
/// Device operating modes based on Export Strategy
/// Each mode controls how excess energy is handled
/// </summary>
public enum DeviceMode
{
    /// <summary>
    /// Sell To Grid - Export excess solar/battery energy to the grid for income
    /// Maps to: ExportStrategySelection = SellingFirst
    /// </summary>
    SellToGrid = 1,

    /// <summary>
    /// Self Consumption - Use all energy at home, prevent any export to grid
    /// Ideal for maximizing self-use and avoiding feed-in restrictions
    /// Maps to: ExportStrategySelection = ZeroExportToLoad
    /// </summary>
    SelfConsumption = 2,

    /// <summary>
    /// Smart Zero Export - Zero export with smart meter/CT sensing for precise control
    /// Uses CT (Current Transformer) readings for accurate grid measurement
    /// Maps to: ExportStrategySelection = ZeroExportToCT
    /// </summary>
    SmartZeroExport = 3
}

/// <summary>
/// Maps DeviceMode to the actual ExportStrategySelection parameter value
/// </summary>
public static class DeviceModeConfigurations
{
    /// <summary>
    /// Get the ExportStrategySelection value for a device mode
    /// </summary>
    public static string? GetExportStrategyValue(DeviceMode mode)
    {
        return ExportStrategyMapping.TryGetValue(mode, out var value) ? value : null;
    }
    };

    /// <summary>
    /// Mode to ExportStrategySelection value mapping
    /// </summary>
    public static readonly Dictionary<DeviceMode, string> ExportStrategyMapping = new()
    {
        [DeviceMode.SellToGrid] = "SellingFirst",
        [DeviceMode.SelfConsumption] = "ZeroExportToLoad",
        [DeviceMode.SmartZeroExport] = "ZeroExportToCT"
    };

    /// <summary>
    /// Get all available modes with their user-friendly descriptions
    /// </summary>
    public static Dictionary<DeviceMode, DeviceModeInfo> GetAllModesWithDescriptions()
    {
        return new Dictionary<DeviceMode, DeviceModeInfo>
        {
            [DeviceMode.SellToGrid] = new DeviceModeInfo
            {
                Name = "Sell to Grid",
                Description = "Export excess solar and battery energy to the grid for income",
                Icon = "sell"
            },
            [DeviceMode.SelfConsumption] = new DeviceModeInfo
            {
                Mode = DeviceMode.SelfConsumption,
                Name = "Self Consumption",
                Description = "Use all energy at home - no export to grid",
                Icon = "home"
            },
            [DeviceMode.SmartZeroExport] = new DeviceModeInfo
            {
                Name = "Smart Zero Export",
                Description = "Zero export with precise smart meter control",
                Icon = "smart"
            }
        };
    }
}

/// <summary>
/// Information about a device mode for display purposes
/// </summary>
public class DeviceModeInfo
{
    public DeviceMode Mode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
