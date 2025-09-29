using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Identity;
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

        // Default data
        // Seed, if necessary
        if (!_context.TodoLists.Any())
        {
            _context.TodoLists.Add(new TodoList
            {
                Title = "Todo List",
                Items =
                {
                    new TodoItem { Title = "Make a todo list 📃" },
                    new TodoItem { Title = "Check off the first item ✅" },
                    new TodoItem { Title = "Realise you've already done two things on the list! 🤯"},
                    new TodoItem { Title = "Reward yourself with a nice, long nap 🏆" },
                }
            });

            await _context.SaveChangesAsync();
        }

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

            // Create SetupDevice relationship (1:1:1 ilişki)
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
    }
}
