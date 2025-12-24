namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service to initialize device settings when a device is added to a setup.
/// Copies parameter values from ParameterValueSettingSchemes to SetupDeviceParameterValues.
/// </summary>
public interface IDeviceSettingsInitializerService
{
    /// <summary>
    /// Initializes device settings for a setup device based on the device model number.
    /// </summary>
    /// <param name="setupDeviceId">The SetupDevice ID</param>
    /// <param name="deviceModelNumber">The device model number (e.g., "three-phase-loftty-12kW")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the number of settings initialized or error information</returns>
    Task<DeviceSettingsInitializationResult> InitializeSettingsAsync(
        Guid setupDeviceId,
        string deviceModelNumber,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of device settings initialization
/// </summary>
public class DeviceSettingsInitializationResult
{
    public bool Success { get; init; }
    public int SettingsCount { get; init; }
    public string? Message { get; init; }
    public string? MatchedSchemeModelNumber { get; init; }

    public static DeviceSettingsInitializationResult NoSchemesFound(string modelNumber) => new()
    {
        Success = false,
        SettingsCount = 0,
        Message = $"No parameter schemes found matching model: {modelNumber}"
    };

    public static DeviceSettingsInitializationResult SettingsAlreadyExist(Guid setupDeviceId) => new()
    {
        Success = false,
        SettingsCount = 0,
        Message = $"Settings already exist for SetupDevice: {setupDeviceId}"
    };

    public static DeviceSettingsInitializationResult Initialized(int count, string matchedModel) => new()
    {
        Success = true,
        SettingsCount = count,
        Message = $"Successfully initialized {count} settings",
        MatchedSchemeModelNumber = matchedModel
    };
}
