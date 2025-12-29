using Hems_Backend.Application.Common.Interfaces;
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
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // At least one of QRCode or SerialNumber must be provided
        if (string.IsNullOrEmpty(request.DeviceQRCode) && string.IsNullOrEmpty(request.DeviceSerialNumber))
        {
            throw new ArgumentException("Either DeviceQRCode or DeviceSerialNumber must be provided");
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
            throw new Hems_Backend.Application.Common.Exceptions.NotFoundException(nameof(Site), request.SiteId);
        }

        // Check if site already has a setup
        var existingSetup = await _context.Setups
            .FirstOrDefaultAsync(s => s.SiteId == request.SiteId, cancellationToken);

        if (existingSetup != null)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException("This site already has a setup. A site can only have one setup.");
        }

        // Find device in ThingsBoard first
        var tbDevice = await _tbConnector.FindDeviceAsync(
            request.DeviceSerialNumber,
            request.DeviceQRCode,
            cancellationToken);

        if (tbDevice == null)
        {
            var deviceIdentifier = !string.IsNullOrEmpty(request.DeviceQRCode)
                ? $"QR Code: {request.DeviceQRCode}"
                : $"Serial Number: {request.DeviceSerialNumber}";

            throw new ArgumentException($"Device not found in ThingsBoard with {deviceIdentifier}");
        }

        // Find device in local database by ThingsBoard ID
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDevice.Id && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            var deviceIdentifier = !string.IsNullOrEmpty(request.DeviceQRCode)
                ? $"QR Code: {request.DeviceQRCode}"
                : $"Serial Number: {request.DeviceSerialNumber}";

            throw new ArgumentException($"Device not found in local database with {deviceIdentifier}");
        }

        // Check if device is already in use
        if (await _context.SetupDevices.AnyAsync(f => f.DeviceId == device.Id, cancellationToken))
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException("This device is already in use.");
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

        await _context.SaveChangesAsync(cancellationToken);

        return setup.Id;
    }
}
