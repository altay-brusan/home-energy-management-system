using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Setups.Commands.CreateSetup;

public record CreateSetupCommand : IRequest<Guid>
{
    public Guid SiteId { get; init; }
    public string Name { get; init; } = "Default Setup";
    public string? DeviceQRCode { get; init; }
    public string? DeviceSerialNumber { get; init; }
}

public class CreateSetupCommandHandler : IRequestHandler<CreateSetupCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IDeviceSettingsInitializerService _deviceSettingsInitializer;
    private readonly IThingsBoardConnectorService _tbConnector;

    public CreateSetupCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IDeviceSettingsInitializerService deviceSettingsInitializer,
        IThingsBoardConnectorService tbConnector)
    {
        _context = context;
        _user = user;
        _deviceSettingsInitializer = deviceSettingsInitializer;
        _tbConnector = tbConnector;
    }

    public async Task<Guid> Handle(CreateSetupCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id
            ?? throw AppException.Unauthorized(MessageCodes.AUTH_USER_NOT_AUTHENTICATED);

        // At least one of QRCode or SerialNumber must be provided
        if (string.IsNullOrEmpty(request.DeviceQRCode) && string.IsNullOrEmpty(request.DeviceSerialNumber))
        {
            throw AppException.BadRequest(MessageCodes.SETUP_MISSING_DEVICE_IDENTIFIER);
        }

        // Site authorization check
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == request.SiteId &&
                                     !s.IsDeleted &&
                                     (s.UserId == userId ||
                                      _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == userId)),
                                 cancellationToken);

        if (site == null)
        {
            throw AppException.NotFound(
                MessageCodes.SITE_NOT_FOUND,
                new Dictionary<string, object> { ["siteId"] = request.SiteId });
        }

        // Check if site already has a setup
        var existingSetup = await _context.Setups
            .FirstOrDefaultAsync(s => s.SiteId == request.SiteId, cancellationToken);

        if (existingSetup != null)
        {
            throw AppException.BadRequest(
                MessageCodes.SETUP_SITE_ALREADY_HAS_SETUP,
                new Dictionary<string, object> { ["siteId"] = request.SiteId });
        }

        // Find device in ThingsBoard first
        var tbDevice = await _tbConnector.FindDeviceAsync(
            request.DeviceSerialNumber,
            request.DeviceQRCode,
            cancellationToken);

        if (tbDevice == null)
        {
            var deviceIdentifier = !string.IsNullOrEmpty(request.DeviceQRCode)
                ? request.DeviceQRCode
                : request.DeviceSerialNumber;

            throw AppException.NotFound(
                MessageCodes.SETUP_DEVICE_NOT_FOUND_THINGSBOARD,
                new Dictionary<string, object> { ["deviceIdentifier"] = deviceIdentifier! });
        }

        // Find device in local database by ThingsBoard ID
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDevice.Id && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            var deviceIdentifier = !string.IsNullOrEmpty(request.DeviceQRCode)
                ? request.DeviceQRCode
                : request.DeviceSerialNumber;

            throw AppException.NotFound(
                MessageCodes.SETUP_DEVICE_NOT_FOUND_LOCAL,
                new Dictionary<string, object> { ["deviceIdentifier"] = deviceIdentifier! });
        }

        // Check if device is already in use
        if (await _context.SetupDevices.AnyAsync(f => f.DeviceId == device.Id, cancellationToken))
        {
            throw AppException.BadRequest(
                MessageCodes.SETUP_DEVICE_ALREADY_IN_USE,
                new Dictionary<string, object> { ["deviceId"] = device.Id });
        }

        // Create setup
        var setup = new Setup
        {
            SiteId = request.SiteId,
            Name = request.Name,
        };

        _context.Setups.Add(setup);

        // Create SetupDevice
        var setupDevice = new SetupDevice
        {
            SetupId = setup.Id,
            DeviceId = device.Id,
        };

        _context.SetupDevices.Add(setupDevice);

        // Initialize device settings from parameter value setting schemes
        if (!string.IsNullOrWhiteSpace(device.ModelNumber))
        {
            await _deviceSettingsInitializer.InitializeSettingsAsync(
                setupDevice.Id,
                device.ModelNumber,
                cancellationToken);
        }

        // Initialize device mode with default value (SelfConsumption)
        // TODO: The mode update logic will be reviewed in future
        await _deviceSettingsInitializer.InitializeModeAsync(
            device.Id,
            cancellationToken: cancellationToken);

        // Grid status tracking starts in Unknown state and will be updated
        // when the first GridStatus telemetry message is received from the device

        await _context.SaveChangesAsync(cancellationToken);

        return setup.Id;
    }
}
