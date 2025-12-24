using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service to initialize device settings when a device is added to a setup.
/// </summary>
public class DeviceSettingsInitializerService : IDeviceSettingsInitializerService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeviceSettingsInitializerService> _logger;

    public DeviceSettingsInitializerService(
        IApplicationDbContext context,
        ILogger<DeviceSettingsInitializerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DeviceSettingsInitializationResult> InitializeSettingsAsync(
        Guid setupDeviceId,
        string deviceModelNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceModelNumber))
        {
            _logger.LogWarning("Cannot initialize settings: device model number is empty for SetupDevice {SetupDeviceId}", setupDeviceId);
            return DeviceSettingsInitializationResult.NoSchemesFound("(empty model number)");
        }

        // Check if settings already exist for this SetupDevice
        var existingSettingsCount = await _context.SetupDeviceParameterValues
            .CountAsync(x => x.SetupDeviceId == setupDeviceId, cancellationToken);

        if (existingSettingsCount > 0)
        {
            _logger.LogInformation(
                "Settings already exist for SetupDevice {SetupDeviceId} ({Count} settings found)",
                setupDeviceId, existingSettingsCount);
            return DeviceSettingsInitializationResult.SettingsAlreadyExist(setupDeviceId);
        }

        // Find matching parameter schemes using Contains check
        // Device model: "three-phase-loftty-12kW" should match scheme model: "three-phase-loftty"
        var allSchemes = await _context.ParameterValueSettingSchemes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Find schemes where the device model number contains the scheme's model number
        // This allows "three-phase-loftty-12kW" to match "three-phase-loftty"
        var matchingSchemes = allSchemes
            .Where(scheme => !string.IsNullOrWhiteSpace(scheme.ModelNumber) &&
                            deviceModelNumber.Contains(scheme.ModelNumber, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingSchemes.Count == 0)
        {
            _logger.LogWarning(
                "No parameter schemes found for device model {DeviceModelNumber}. Checked {TotalSchemes} schemes.",
                deviceModelNumber, allSchemes.Count);
            return DeviceSettingsInitializationResult.NoSchemesFound(deviceModelNumber);
        }

        // Get the matched model number (for logging/result)
        var matchedModelNumber = matchingSchemes.First().ModelNumber;

        _logger.LogInformation(
            "Found {Count} parameter schemes matching model '{MatchedModel}' for device model '{DeviceModel}'",
            matchingSchemes.Count, matchedModelNumber, deviceModelNumber);

        // Create SetupDeviceParameterValues from the matching schemes
        var parameterValues = matchingSchemes.Select(scheme => new SetupDeviceParameterValue
        {
            SetupDeviceId = setupDeviceId,
            ParameterId = scheme.ParameterId,
            ParameterGroupId = scheme.ParameterGroupId,
            Value = scheme.Value,
            SortOrder = scheme.SortOrder,
            ModelNumber = scheme.ModelNumber
        }).ToList();

        _context.SetupDeviceParameterValues.AddRange(parameterValues);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Initialized {Count} settings for SetupDevice {SetupDeviceId} from model '{MatchedModel}'",
            parameterValues.Count, setupDeviceId, matchedModelNumber);

        return DeviceSettingsInitializationResult.Initialized(parameterValues.Count, matchedModelNumber);
    }
}
