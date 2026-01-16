using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Identity;
using Hems_Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();

        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole(Roles.Administrator);

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        // Default users
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "***REMOVED***");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new [] { administratorRole.Name });
            }
        }

        // Seed MobilUser role for demo user
        var mobilUserRole = new IdentityRole(Roles.MobilUser);
        if (_roleManager.Roles.All(r => r.Name != mobilUserRole.Name))
        {
            await _roleManager.CreateAsync(mobilUserRole);
        }

        // Demo user for app store assessment (Google Play Store / iOS App Store)
        var demoUser = new ApplicationUser
        {
            UserName = DemoDataService.DemoUserEmail,
            Email = DemoDataService.DemoUserEmail,
            FirstName = "Demo",
            Surname = "User",
            IsDemo = true,
            EmailConfirmed = true // Demo user doesn't need email confirmation
        };

        if (_userManager.Users.All(u => u.UserName != demoUser.UserName))
        {
            var result = await _userManager.CreateAsync(demoUser, "Demo@123");
            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(mobilUserRole.Name))
                {
                    await _userManager.AddToRolesAsync(demoUser, new[] { mobilUserRole.Name });
                }
                _logger.LogInformation("Demo user created: {Email}", demoUser.Email);
            }
            else
            {
                _logger.LogWarning("Failed to create demo user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Seed demo device at startup (so simulator can work immediately)
        await SeedDemoDeviceAsync();

        // Seed Device data
        if (!_context.Devices.Any())
        {
            // Create a default site first
            var defaultSite = new Site
            {
                UserId = administrator.Id,
                Name = "Default Site",
                Country = "Turkey",
                City = "Istanbul",
                AddressLine = "Test Address",
                Latitude = 41.0082m,
                Longitude = 28.9784m,
                Timezone = "Europe/Istanbul2",
                CreatedBy= administrator.Id
            };

            _context.Sites.Add(defaultSite);
            await _context.SaveChangesAsync();

            // Create a default setup
            var defaultSetup = new Setup
            {
                SiteId = defaultSite.Id,
                Name = "Default Setup",
                CreatedBy = administrator.Id
            };

            _context.Setups.Add(defaultSetup);
            await _context.SaveChangesAsync();

            // Create sample device
            var device = new Device
            {
                Manufacturer = "Siemens",
                ModelNumber = "S7-1200",
                SerialNumber = "SN123456789",
                QRCode = "QR123456789",
                TypeNumber = "6ES7214-1AG40-0XB0",
                Specifications = System.Text.Json.JsonDocument.Parse("{\"cpu\":\"CPU 1214C\",\"memory\":\"100KB\",\"inputs\":\"14\",\"outputs\":\"10\"}"),
                InstallationDate = DateTime.UtcNow.AddDays(-30),
                CalibrationDate = DateTime.UtcNow.AddDays(-15),
                NextCheckoutDate = DateTime.UtcNow.AddDays(15),
                WarrantyPeriod = 24,
                WarrantyUnit = Domain.Enums.WarrantyUnit.Month,
                WarrantyStartDate = DateTime.UtcNow.AddDays(-30),
                CreatedBy = administrator.Id
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            // Create SetupDevice relationship (1:1:1 iliski)
            var setupDevice = new SetupDevice
            {
                SetupId = defaultSetup.Id,
                DeviceId = device.Id,
                CreatedBy = administrator.Id
            };

            _context.SetupDevices.Add(setupDevice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded 1 device");
        }

        // Seed Device Notification Types
        // NOTE: The actual notification types are populated via SQL script (scripts/add_deye_notification_types.sql)
        // This seed data only contains generic fallback types for backward compatibility
        if (!_context.DeviceNotificationTypes.Any())
        {
            var notificationTypes = new List<DeviceNotificationType>
            {
                // INVERTER - Generic fallback types
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INV-ALM-GENERIC",
                    Message = "Inverter Alarm",
                    Description = "A generic inverter alarm was detected. Please contact technical support for details.",
                    Category = "Inverter",
                    Severity = "ALARM",
                    Version = "1.0",
                    Manufacturer = "Generic",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INV-WAR-GENERIC",
                    Message = "Inverter Warning",
                    Description = "A generic inverter warning was detected. Please contact technical support for details.",
                    Category = "Inverter",
                    Severity = "WARNING",
                    Version = "1.0",
                    Manufacturer = "Generic",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INV-ERR-GENERIC",
                    Message = "Inverter Error",
                    Description = "A generic inverter error was detected. Please contact technical support for details.",
                    Category = "Inverter",
                    Severity = "ERROR",
                    Version = "1.0",
                    Manufacturer = "Generic",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                // BMS - Generic fallback types
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "BMS-ALM-GENERIC",
                    Message = "BMS Alarm",
                    Description = "A generic BMS alarm was detected. Please contact technical support for details.",
                    Category = "Battery",
                    Severity = "ALARM",
                    Version = "1.0",
                    Manufacturer = "Generic",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "BMS-WAR-GENERIC",
                    Message = "BMS Warning",
                    Description = "A generic BMS warning was detected. Please contact technical support for details.",
                    Category = "Battery",
                    Severity = "WARNING",
                    Version = "1.0",
                    Manufacturer = "Generic",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "BMS-ERR-GENERIC",
                    Message = "BMS Error",
                    Description = "A generic BMS error was detected. Please contact technical support for details.",
                    Category = "Battery",
                    Severity = "ERROR",
                    Version = "1.0",
                    Manufacturer = "Generic",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                }
            };

            _context.DeviceNotificationTypes.AddRange(notificationTypes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} device notification types", notificationTypes.Count);
        }

        // Seed Countries and Cities
        if (!_context.Countries.Any())
        {
            var countries = new List<Country>
            {
                new Country
                {
                    Id = Guid.NewGuid(),
                    Name = "T�rkiye",
                    Code = "TR",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new Country
                {
                    Id = Guid.NewGuid(),
                    Name = "United States",
                    Code = "US",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new Country
                {
                    Id = Guid.NewGuid(),
                    Name = "Germany",
                    Code = "DE",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new Country
                {
                    Id = Guid.NewGuid(),
                    Name = "France",
                    Code = "FR",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new Country
                {
                    Id = Guid.NewGuid(),
                    Name = "United Kingdom",
                    Code = "GB",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                }
            };

            _context.Countries.AddRange(countries);
            await _context.SaveChangesAsync();

            // Get the country IDs for cities
            var turkeyId = countries.First(c => c.Code == "TR").Id;
            var usaId = countries.First(c => c.Code == "US").Id;
            var germanyId = countries.First(c => c.Code == "DE").Id;
            var franceId = countries.First(c => c.Code == "FR").Id;
            var ukId = countries.First(c => c.Code == "GB").Id;

            var cities = new List<City>
            {
                // Turkey Cities
                new City { Id = Guid.NewGuid(), Name = "Istanbul", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Ankara", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Izmir", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Bursa", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Antalya", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Adana", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Konya", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Gaziantep", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Sanliurfa", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Kocaeli", CountryId = turkeyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },

                // USA Cities
                new City { Id = Guid.NewGuid(), Name = "New York", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Los Angeles", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Chicago", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Houston", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Phoenix", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Philadelphia", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "San Antonio", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "San Diego", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Dallas", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "San Jose", CountryId = usaId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },

                // Germany Cities
                new City { Id = Guid.NewGuid(), Name = "Berlin", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Hamburg", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Munich", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Cologne", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Frankfurt", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Stuttgart", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "D�sseldorf", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Dortmund", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Essen", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Leipzig", CountryId = germanyId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },

                // France Cities
                new City { Id = Guid.NewGuid(), Name = "Paris", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Marseille", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Lyon", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Toulouse", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Nice", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Nantes", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Strasbourg", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Montpellier", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Bordeaux", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Lille", CountryId = franceId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },

                // UK Cities
                new City { Id = Guid.NewGuid(), Name = "London", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Birmingham", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Manchester", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Glasgow", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Liverpool", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Leeds", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Sheffield", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Edinburgh", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Bristol", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id },
                new City { Id = Guid.NewGuid(), Name = "Cardiff", CountryId = ukId, IsActive = true, Created = DateTimeOffset.UtcNow, CreatedBy = administrator.Id }
            };

            _context.Cities.AddRange(cities);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {CountryCount} countries and {CityCount} cities", countries.Count, cities.Count);
        }
    }

    /// <summary>
    /// Seeds demo device at startup so simulator can work immediately.
    /// This is called during application startup, not on demo user login.
    /// </summary>
    private async Task SeedDemoDeviceAsync()
    {
        try
        {
            // Check if demo device already exists
            var existingDemoDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.SerialNumber == DemoDataService.DemoDeviceSerial);

            if (existingDemoDevice != null)
            {
                _logger.LogDebug("Demo device already exists: {SerialNumber}", DemoDataService.DemoDeviceSerial);
                return;
            }

            // Get demo user
            var demoUser = await _userManager.FindByEmailAsync(DemoDataService.DemoUserEmail);
            if (demoUser == null)
            {
                _logger.LogWarning("Demo user not found, skipping demo device seeding");
                return;
            }

            _logger.LogInformation("Seeding demo device for simulator...");

            // Create demo site
            var demoSite = new Site
            {
                UserId = demoUser.Id,
                Name = "Demo Home",
                Country = "Turkiye",
                City = "Istanbul",
                District = "Kadikoy",
                AddressLine = "Demo Address 123",
                Latitude = 40.9885m,
                Longitude = 29.0288m,
                Timezone = "Europe/Istanbul",
                CreatedBy = demoUser.Id
            };

            _context.Sites.Add(demoSite);
            await _context.SaveChangesAsync();

            // Create demo setup
            var demoSetup = new Setup
            {
                SiteId = demoSite.Id,
                Name = "Demo Setup",
                CreatedBy = demoUser.Id
            };

            _context.Setups.Add(demoSetup);
            await _context.SaveChangesAsync();

            // Create demo device with well-known ThingsBoardDeviceId
            var demoDevice = new Device
            {
                Name = "Demo HEMS Device",
                Manufacturer = "Oklidis",
                ModelNumber = "HEMS-DEMO-001",
                SerialNumber = DemoDataService.DemoDeviceSerial,
                TypeNumber = "DEMO-TYPE-001",
                QRCode = $"DEMO-QR-{Guid.NewGuid():N}",
                Specifications = System.Text.Json.JsonDocument.Parse("{\"type\":\"demo\",\"capacity\":\"10kW\"}"),
                InstallationDate = DateTime.UtcNow.AddDays(-30),
                WarrantyPeriod = 24,
                WarrantyUnit = Domain.Enums.WarrantyUnit.Month,
                WarrantyStartDate = DateTime.UtcNow.AddDays(-30),
                IsDemoDevice = true,
                ThingsBoardDeviceId = DemoDataService.DemoThingsBoardDeviceId,
                CreatedBy = demoUser.Id
            };

            _context.Devices.Add(demoDevice);
            await _context.SaveChangesAsync();

            // Create setup-device relationship
            var setupDevice = new SetupDevice
            {
                SetupId = demoSetup.Id,
                DeviceId = demoDevice.Id,
                CreatedBy = demoUser.Id
            };

            _context.SetupDevices.Add(setupDevice);
            await _context.SaveChangesAsync();

            // Create demo device components
            var components = new List<DeviceComponent>
            {
                new DeviceComponent
                {
                    DeviceId = demoDevice.Id,
                    DeviceType = Domain.Enums.DeviceType.Invertor,
                    Name = "Demo Inverter",
                    Manufacturer = "Oklidis",
                    ModelNumber = "INV-DEMO-5000",
                    SerialNumber = "DEMO-INV-001",
                    Specifications = System.Text.Json.JsonDocument.Parse("{\"power\":\"5kW\",\"efficiency\":\"98%\"}"),
                    CreatedBy = demoUser.Id
                },
                new DeviceComponent
                {
                    DeviceId = demoDevice.Id,
                    DeviceType = Domain.Enums.DeviceType.Battery,
                    Name = "Demo Battery",
                    Manufacturer = "Oklidis",
                    ModelNumber = "BAT-DEMO-10000",
                    SerialNumber = "DEMO-BAT-001",
                    Specifications = System.Text.Json.JsonDocument.Parse("{\"capacity\":\"10kWh\",\"type\":\"LiFePO4\"}"),
                    CreatedBy = demoUser.Id
                },
                new DeviceComponent
                {
                    DeviceId = demoDevice.Id,
                    DeviceType = Domain.Enums.DeviceType.BMS,
                    Name = "Demo BMS",
                    Manufacturer = "Oklidis",
                    ModelNumber = "BMS-DEMO-001",
                    SerialNumber = "DEMO-BMS-001",
                    Specifications = System.Text.Json.JsonDocument.Parse("{\"cells\":\"16\",\"voltage\":\"51.2V\"}"),
                    CreatedBy = demoUser.Id
                }
            };

            _context.DeviceComponents.AddRange(components);
            await _context.SaveChangesAsync();

            // Initialize device mode
            var defaultMode = new DeviceModeHistory
            {
                DeviceId = demoDevice.Id,
                Mode = (int)Application.DeviceSettings.Models.DeviceMode.SelfConsumption,
                Description = "Self Consumption",
                IsActive = true
            };
            _context.DeviceModeHistory.Add(defaultMode);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Demo device seeded: Site={SiteId}, Setup={SetupId}, Device={DeviceId}, SerialNumber={SerialNumber}",
                demoSite.Id, demoSetup.Id, demoDevice.Id, demoDevice.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding demo device");
            // Don't throw - this is optional seeding
        }
    }
}
