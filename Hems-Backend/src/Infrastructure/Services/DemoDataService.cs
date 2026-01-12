using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using Hems_Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for managing demo user data.
/// Handles data reset on login and provides demo-specific operations.
/// </summary>
public class DemoDataService : IDemoDataService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DemoDataService> _logger;

    // Well-known demo user constants
    public const string DemoUserEmail = "demo@oklidis.com.tr";
    public const string DemoDeviceSerial = "DEMO-HEMS-001";

    // Well-known ThingsBoard device ID for demo device
    // This is used to identify demo device data in telemetry tables
    public static readonly Guid DemoThingsBoardDeviceId =
        new Guid("00000000-de00-0000-0000-000000000001");

    public DemoDataService(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<DemoDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public string GetDemoUserEmail() => DemoUserEmail;
    public string GetDemoDeviceSerialNumber() => DemoDeviceSerial;

    public async Task<bool> IsDemoUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.IsDemo ?? false;
    }

    public async Task<bool> IsDemoUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.IsDemo ?? false;
    }

    public async Task<bool> IsDemoDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        var isDemoDevice = device?.IsDemoDevice ?? false;

        _logger.LogDebug(
            "IsDemoDeviceAsync: DeviceId={DeviceId}, DeviceFound={DeviceFound}, IsDemoDevice={IsDemoDevice}",
            deviceId, device != null, isDemoDevice);

        return isDemoDevice;
    }

    public async Task<DemoDataResetResult> ResetDemoDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting demo data reset for user {UserId}", userId);

            // Verify this is a demo user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsDemo)
            {
                return new DemoDataResetResult
                {
                    Success = false,
                    ErrorMessage = "User is not a demo user"
                };
            }

            // Get all sites owned by demo user
            var demoSites = await _context.Sites
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .Include(s => s.Setup)
                    .ThenInclude(setup => setup!.SetupDevice)
                        .ThenInclude(sd => sd!.Device)
                .ToListAsync(cancellationToken);

            // Get all demo device ThingsBoardDeviceIds (data tables use ThingsBoardDeviceId)
            var demoDevices = demoSites
                .Where(s => s.Setup?.SetupDevice?.Device != null)
                .Select(s => s.Setup!.SetupDevice!.Device)
                .ToList();

            var demoDeviceIds = demoDevices.Select(d => d.Id).ToList();
            var demoThingsBoardDeviceIds = demoDevices.Select(d => d.ThingsBoardDeviceId).ToList();

            int telemetryDeleted = 0;
            int notificationsDeleted = 0;
            int energyDataDeleted = 0;

            // Delete telemetry data for demo devices (using ThingsBoardDeviceId)
            if (demoThingsBoardDeviceIds.Any())
            {
                var deviceData = await _context.DeviceData
                    .Where(d => demoThingsBoardDeviceIds.Contains(d.ThingsBoardDeviceId))
                    .ToListAsync(cancellationToken);
                _context.DeviceData.RemoveRange(deviceData);
                telemetryDeleted += deviceData.Count;

                var energyData = await _context.DeviceEnergyData
                    .Where(d => demoThingsBoardDeviceIds.Contains(d.ThingsBoardDeviceId))
                    .ToListAsync(cancellationToken);
                _context.DeviceEnergyData.RemoveRange(energyData);
                energyDataDeleted += energyData.Count;

                var powerData = await _context.DevicePowerData
                    .Where(d => demoThingsBoardDeviceIds.Contains(d.ThingsBoardDeviceId))
                    .ToListAsync(cancellationToken);
                _context.DevicePowerData.RemoveRange(powerData);
                telemetryDeleted += powerData.Count;

                var weatherData = await _context.DeviceWeatherData
                    .Where(d => demoThingsBoardDeviceIds.Contains(d.ThingsBoardDeviceId))
                    .ToListAsync(cancellationToken);
                _context.DeviceWeatherData.RemoveRange(weatherData);
                telemetryDeleted += weatherData.Count;

                var disconnectLogs = await _context.DeviceDisconnectLogs
                    .Where(d => demoThingsBoardDeviceIds.Contains(d.ThingsBoardDeviceId))
                    .ToListAsync(cancellationToken);
                _context.DeviceDisconnectLogs.RemoveRange(disconnectLogs);
                telemetryDeleted += disconnectLogs.Count;
            }

            // Delete device notifications and mode history (using DeviceId)
            if (demoDeviceIds.Any())
            {
                var deviceNotifications = await _context.DeviceNotifications
                    .Where(d => demoDeviceIds.Contains(d.DeviceId))
                    .ToListAsync(cancellationToken);
                _context.DeviceNotifications.RemoveRange(deviceNotifications);
                notificationsDeleted += deviceNotifications.Count;

                // Reset device mode history - keep only the default mode
                var modeHistory = await _context.DeviceModeHistory
                    .Where(d => demoDeviceIds.Contains(d.DeviceId))
                    .ToListAsync(cancellationToken);
                _context.DeviceModeHistory.RemoveRange(modeHistory);

                // Re-add default mode
                foreach (var deviceId in demoDeviceIds)
                {
                    var defaultMode = new DeviceModeHistory
                    {
                        DeviceId = deviceId,
                        Mode = (int)Application.DeviceSettings.Models.DeviceMode.SelfConsumption,
                        Description = "Self Consumption",
                        IsActive = true
                    };
                    _context.DeviceModeHistory.Add(defaultMode);
                }
            }

            // Delete user notifications
            var userNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync(cancellationToken);
            _context.Notifications.RemoveRange(userNotifications);
            notificationsDeleted += userNotifications.Count;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Demo data reset completed for user {UserId}: {Telemetry} telemetry, {Notifications} notifications, {Energy} energy records deleted",
                userId, telemetryDeleted, notificationsDeleted, energyDataDeleted);

            return new DemoDataResetResult
            {
                Success = true,
                TelemetryRecordsDeleted = telemetryDeleted,
                NotificationsDeleted = notificationsDeleted,
                EnergyDataRecordsDeleted = energyDataDeleted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting demo data for user {UserId}", userId);
            return new DemoDataResetResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DemoDataSeedResult> SeedDemoDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting demo data seeding for user {UserId}", userId);

            // Verify this is a demo user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsDemo)
            {
                return new DemoDataSeedResult
                {
                    Success = false,
                    ErrorMessage = "User is not a demo user"
                };
            }

            // Check if demo data already exists
            var existingSite = await _context.Sites
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken);

            if (existingSite != null)
            {
                _logger.LogInformation("Demo data already exists for user {UserId}", userId);

                var existingSetup = await _context.Setups
                    .FirstOrDefaultAsync(s => s.SiteId == existingSite.Id, cancellationToken);

                var existingDevice = existingSetup != null
                    ? await _context.SetupDevices
                        .Where(sd => sd.SetupId == existingSetup.Id)
                        .Select(sd => sd.DeviceId)
                        .FirstOrDefaultAsync(cancellationToken)
                    : Guid.Empty;

                return new DemoDataSeedResult
                {
                    Success = true,
                    SiteId = existingSite.Id,
                    SetupId = existingSetup?.Id,
                    DeviceId = existingDevice != Guid.Empty ? existingDevice : null
                };
            }

            // Create demo site
            var demoSite = new Site
            {
                UserId = userId,
                Name = "Demo Home",
                Country = "Turkiye",
                City = "Istanbul",
                District = "Kadikoy",
                AddressLine = "Demo Address 123",
                Latitude = 40.9885m,
                Longitude = 29.0288m,
                Timezone = "Europe/Istanbul",
                CreatedBy = userId
            };

            _context.Sites.Add(demoSite);
            await _context.SaveChangesAsync(cancellationToken);

            // Create demo setup
            var demoSetup = new Setup
            {
                SiteId = demoSite.Id,
                Name = "Demo Setup",
                CreatedBy = userId
            };

            _context.Setups.Add(demoSetup);
            await _context.SaveChangesAsync(cancellationToken);

            // Create demo device (using well-known ThingsBoardDeviceId for data identification)
            var demoDevice = new Device
            {
                Name = "Demo HEMS Device",
                Manufacturer = "Oklidis",
                ModelNumber = "HEMS-DEMO-001",
                SerialNumber = DemoDeviceSerial,
                TypeNumber = "DEMO-TYPE-001",
                QRCode = $"DEMO-QR-{Guid.NewGuid():N}",
                Specifications = System.Text.Json.JsonDocument.Parse("{\"type\":\"demo\",\"capacity\":\"10kW\"}"),
                InstallationDate = DateTime.UtcNow.AddDays(-30),
                WarrantyPeriod = 24,
                WarrantyUnit = WarrantyUnit.Month,
                WarrantyStartDate = DateTime.UtcNow.AddDays(-30),
                IsDemoDevice = true,
                ThingsBoardDeviceId = DemoThingsBoardDeviceId, // Well-known ID for simulator targeting
                CreatedBy = userId
            };

            _context.Devices.Add(demoDevice);
            await _context.SaveChangesAsync(cancellationToken);

            // Create setup-device relationship
            var setupDevice = new SetupDevice
            {
                SetupId = demoSetup.Id,
                DeviceId = demoDevice.Id,
                CreatedBy = userId
            };

            _context.SetupDevices.Add(setupDevice);
            await _context.SaveChangesAsync(cancellationToken);

            // Create demo device components
            var components = new List<DeviceComponent>
            {
                new DeviceComponent
                {
                    DeviceId = demoDevice.Id,
                    DeviceType = DeviceType.Invertor,
                    Name = "Demo Inverter",
                    Manufacturer = "Oklidis",
                    ModelNumber = "INV-DEMO-5000",
                    SerialNumber = "DEMO-INV-001",
                    Specifications = System.Text.Json.JsonDocument.Parse("{\"power\":\"5kW\",\"efficiency\":\"98%\"}"),
                    CreatedBy = userId
                },
                new DeviceComponent
                {
                    DeviceId = demoDevice.Id,
                    DeviceType = DeviceType.Battery,
                    Name = "Demo Battery",
                    Manufacturer = "Oklidis",
                    ModelNumber = "BAT-DEMO-10000",
                    SerialNumber = "DEMO-BAT-001",
                    Specifications = System.Text.Json.JsonDocument.Parse("{\"capacity\":\"10kWh\",\"type\":\"LiFePO4\"}"),
                    CreatedBy = userId
                },
                new DeviceComponent
                {
                    DeviceId = demoDevice.Id,
                    DeviceType = DeviceType.BMS,
                    Name = "Demo BMS",
                    Manufacturer = "Oklidis",
                    ModelNumber = "BMS-DEMO-001",
                    SerialNumber = "DEMO-BMS-001",
                    Specifications = System.Text.Json.JsonDocument.Parse("{\"cells\":\"16\",\"voltage\":\"51.2V\"}"),
                    CreatedBy = userId
                }
            };

            _context.DeviceComponents.AddRange(components);
            await _context.SaveChangesAsync(cancellationToken);

            // Initialize device mode
            var defaultMode = new DeviceModeHistory
            {
                DeviceId = demoDevice.Id,
                Mode = (int)Application.DeviceSettings.Models.DeviceMode.SelfConsumption,
                Description = "Self Consumption",
                IsActive = true
            };
            _context.DeviceModeHistory.Add(defaultMode);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Demo data seeded for user {UserId}: Site={SiteId}, Setup={SetupId}, Device={DeviceId}",
                userId, demoSite.Id, demoSetup.Id, demoDevice.Id);

            return new DemoDataSeedResult
            {
                Success = true,
                SiteId = demoSite.Id,
                SetupId = demoSetup.Id,
                DeviceId = demoDevice.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding demo data for user {UserId}", userId);
            return new DemoDataSeedResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
