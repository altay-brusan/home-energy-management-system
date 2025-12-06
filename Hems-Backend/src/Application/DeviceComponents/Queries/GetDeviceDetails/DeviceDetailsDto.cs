namespace Hems_Backend.Application.DeviceComponents.Queries.GetDeviceDetails;

/// <summary>
/// Response containing device details organized into realtime and parameter groups
/// </summary>
public class DeviceDetailsResponseDto
{
    /// <summary>
    /// List of all detail items (realtime + parameters)
    /// </summary>
    public List<DeviceDetailItemDto> Details { get; set; } = new();
}

/// <summary>
/// Individual detail item with key, value, unit, and label
/// </summary>
public class DeviceDetailItemDto
{
    /// <summary>
    /// Unique key for this detail (e.g., "soc", "outputPower", "model")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value of this detail (can be number, string, boolean)
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., "%", "W", "V", "A", "Hz", "°C", null for no unit)
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Label key for frontend localization (e.g., "realtime.soc", "parameter.model")
    /// Starts with "realtime." for live data or "parameter." for static configuration
    /// </summary>
    public string LabelKey { get; set; } = string.Empty;
}
