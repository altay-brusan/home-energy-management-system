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
                    new TodoItem { Title = "Make a todo list ??" },
                    new TodoItem { Title = "Check off the first item ?" },
                    new TodoItem { Title = "Realise you've already done two things on the list! ??"},
                    new TodoItem { Title = "Reward yourself with a nice, long nap ??" },
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
        if (!_context.DeviceNotificationTypes.Any())
        {
            var notificationTypes = new List<DeviceNotificationType>
            {
                // INVERTER - INFO / DURUM BILGILERI
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0001",
                    Message = "Standby",
                    Status = "Standby",
                    Description = "Inverter hazirda beklemede. PV veya Batarya bagli degil ya da \"enable\" edilmemis.",
                    Register = 500,
                    Value = 0,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0002",
                    Message = "Selfcheck",
                    Status = "Selfcheck",
                    Description = "Inverter a�ilis �ncesi i� testleri yapiyor. Normal baslangi� asamasi.",
                    Register = 500,
                    Value = 1,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0003",
                    Message = "Normal",
                    Status = "Normal",
                    Description = "Ideal durum. Inverter sorunsuz �alisiyor, enerji d�n�s�m� yapiyor.",
                    Register = 500,
                    Value = 2,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0001",
                    Message = "Alarm",
                    Status = "Alarm",
                    Description = "Inverter bir uyari (warning) durumunda ama �alismaya devam ediyor. EMS, Reg.553�554 araligini izlemeye devam etmeli.",
                    Register = 500,
                    Value = 3,
                    Category = "WARNING",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0001",
                    Message = "Fault",
                    Status = "Fault",
                    Description = "Kritik durum. Bir hata olustu, inverter durdu veya durmak �zere. EMS, Reg.555�558 araligindaki fault kelimelerini incelemeli.",
                    Register = 500,
                    Value = 4,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0004",
                    Message = "Activating",
                    Status = "Activating",
                    Description = "Inverter a�iliyor ve sebekeye senkronize oluyor. Ge�ici durum.",
                    Register = 500,
                    Value = 5,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // ANAHTAR / ENABLE DURUMLARI
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0005",
                    Message = "Switch On/Off Enable",
                    Status = "Switch On/Off Enable",
                    Description = "Inverter \"ON\" komutunda. �alismaya hazir.",
                    Register = 080,
                    Value = 1,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0006",
                    Message = "Reactive Power Enable",
                    Status = "Reactive Power Enable",
                    Description = "Inverter'in �alisma modu: 0 ? Watt, 1 ? PF, 2 ? VAr.",
                    Register = 084,
                    Value = 0,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0007",
                    Message = "AC Couple Enable",
                    Status = "AC Couple Enable",
                    Description = "AC coupling �zelligi aktif. AC kaynak �zerinden batarya sarj edilebilir.",
                    Register = 234,
                    Value = 1,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0008",
                    Message = "Energy Management Mode",
                    Status = "Energy Management Mode",
                    Description = "\"Battery First\" (10) veya \"Load First\" (11) modlarini temsil eder.",
                    Register = 141,
                    Value = 10,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // R�LE DURUMLARI (AC Relay Status � Register 552)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0009",
                    Message = "INV Relay",
                    Status = "INV Relay",
                    Description = "Inverter �ikisi devrede. Y�k beslemesi aktif.",
                    Register = 552,
                    Value = 0,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0010",
                    Message = "Grid Relay",
                    Status = "Grid Relay",
                    Description = "Sebeke (Grid) devrede, senkronize durumda.",
                    Register = 552,
                    Value = 2,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0011",
                    Message = "Gen Relay",
                    Status = "Gen Relay",
                    Description = "Jenerat�r devrede ve inverter tarafindan kullanilabilir durumda.",
                    Register = 552,
                    Value = 3,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // INVERTER FAULT / WARNING LISTESI
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0002",
                    Message = "Fan Warning",
                    Status = "Fan Warning",
                    Description = "Fan hizi d�s�k / arizali. Inverter sicakligi artabilir.",
                    Register = 553,
                    Value = 1,
                    Category = "WARNING",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0003",
                    Message = "Grid Phase Wrong",
                    Status = "Grid Phase Wrong",
                    Description = "Faz sirasi hatali. Grid baglantisi yapilamaz veya kesilir.",
                    Register = 553,
                    Value = 2,
                    Category = "WARNING",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0004",
                    Message = "Grid Under Voltage Warning",
                    Status = "Grid Under Voltage Warning",
                    Description = "Sebeke voltaji d�s�k limitlere yaklasiyor.",
                    Register = 553,
                    Value = 3,
                    Category = "WARNING",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0005",
                    Message = "Grid Over Voltage Warning",
                    Status = "Grid Over Voltage Warning",
                    Description = "Sebeke voltaji y�ksek limitlere yaklasiyor.",
                    Register = 553,
                    Value = 4,
                    Category = "WARNING",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0006",
                    Message = "Lithium Battery Lost Warning",
                    Status = "Lithium Battery Lost Warning",
                    Description = "BMS ile haberlesme kesildi veya batarya baglantisi koptu.",
                    Register = 554,
                    Value = 14,
                    Category = "WARNING",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0007",
                    Message = "Parallel Comm Quality Warning",
                    Status = "Parallel Comm Quality Warning",
                    Description = "Paralel �alisan inverterler arasinda haberlesme kalitesi d�s�k.",
                    Register = 554,
                    Value = 15,
                    Category = "WARNING",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // FAULT CODES
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0002",
                    Message = "PV Over-Voltage Fault",
                    Status = "PV Over-Voltage Fault",
                    Description = "PV giris voltaji �ok y�ksek.",
                    Register = 555,
                    Value = 0,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0003",
                    Message = "Grid Over-Voltage Fault",
                    Status = "Grid Over-Voltage Fault",
                    Description = "Sebeke voltaji izin verilen maksimum degeri asti.",
                    Register = 555,
                    Value = 1,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0004",
                    Message = "Grid Under-Voltage Fault",
                    Status = "Grid Under-Voltage Fault",
                    Description = "Sebeke voltaji izin verilen minimum degerin altina d�st�.",
                    Register = 555,
                    Value = 2,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0005",
                    Message = "Grid Frequency Abnormal",
                    Status = "Grid Frequency Abnormal",
                    Description = "Sebeke frekansi izin verilen bandin disinda.",
                    Register = 555,
                    Value = 3,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0006",
                    Message = "Bus Over-Voltage Fault",
                    Status = "Bus Over-Voltage Fault",
                    Description = "DC Bus voltaji tehlikeli seviyede. Acil shutdown.",
                    Register = 555,
                    Value = 4,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0007",
                    Message = "Bus Under-Voltage Fault",
                    Status = "Bus Under-Voltage Fault",
                    Description = "DC Bus voltaji �ok d�s�k.",
                    Register = 555,
                    Value = 5,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0008",
                    Message = "Over-Temperature Fault",
                    Status = "Over-Temperature Fault",
                    Description = "Inverter asiri isindi.",
                    Register = 555,
                    Value = 6,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0009",
                    Message = "Output Short-Circuit Fault",
                    Status = "Output Short-Circuit Fault",
                    Description = "Inverter �ikisinda kisa devre tespit edildi.",
                    Register = 555,
                    Value = 7,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0010",
                    Message = "Battery Over-Voltage Fault",
                    Status = "Battery Over-Voltage Fault",
                    Description = "Batarya sarj voltaji �ok y�ksek.",
                    Register = 556,
                    Value = 2,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0011",
                    Message = "Battery Under-Voltage Fault",
                    Status = "Battery Under-Voltage Fault",
                    Description = "Batarya voltaji �ok d�s�k.",
                    Register = 556,
                    Value = 3,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0012",
                    Message = "Battery Over-Current Fault",
                    Status = "Battery Over-Current Fault",
                    Description = "Batarya akimi limiti asti.",
                    Register = 556,
                    Value = 4,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0013",
                    Message = "BMS Communication Fault",
                    Status = "BMS Communication Fault",
                    Description = "BMS ile s�rekli bir haberlesme hatasi.",
                    Register = 556,
                    Value = 6,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0014",
                    Message = "Relay Fault",
                    Status = "Relay Fault",
                    Description = "Ana r�le a�ilamadi veya kapanamadi.",
                    Register = 557,
                    Value = 0,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0015",
                    Message = "Isolation Fault",
                    Status = "Isolation Fault",
                    Description = "PV veya batarya tarafinda toprak ka�ak akimi.",
                    Register = 557,
                    Value = 2,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0016",
                    Message = "Grid Relay Fault",
                    Status = "Grid Relay Fault",
                    Description = "Sebeke baglanti r�lesi arizali.",
                    Register = 557,
                    Value = 4,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0017",
                    Message = "DSP Communication Fault",
                    Status = "DSP Communication Fault",
                    Description = "Ana kontrol�r (MCU) ile g�� katmani arasinda haberlesme kesildi.",
                    Register = 558,
                    Value = 0,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-INV-0018",
                    Message = "EEPROM Fault",
                    Status = "EEPROM Fault",
                    Description = "EEPROM okunamiyor veya yazilamiyor.",
                    Register = 558,
                    Value = 2,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // �ALISMA LIMITLERI (Operation Limits � CAN ID 0x351)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0001",
                    Message = "Charge Voltage Limit",
                    Status = "Charge Voltage Limit",
                    Description = "Bataryanin su anda kabul edebilecegi maksimum sarj voltaji.",
                    Register = 0x351,
                    Value = 0,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0002",
                    Message = "Charge Current Limit",
                    Status = "Charge Current Limit",
                    Description = "Bataryanin kabul edebilecegi maksimum sarj akimi.",
                    Register = 0x351,
                    Value = 1,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0003",
                    Message = "Discharge Current Limit",
                    Status = "Discharge Current Limit",
                    Description = "Bataryanin saglayabilecegi maksimum desarj akimi.",
                    Register = 0x351,
                    Value = 2,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0004",
                    Message = "Discharge Voltage Limit",
                    Status = "Discharge Voltage Limit",
                    Description = "Bataryanin izin verilen minimum desarj voltaji.",
                    Register = 0x351,
                    Value = 3,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // SISTEM SAGLIK �L��MLERI (CAN ID 0x355, 0x356)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0005",
                    Message = "Soc",
                    Status = "Soc",
                    Description = "Batarya sarj seviyesi (%).",
                    Register = 0x355,
                    Value = 0,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0006",
                    Message = "Soh",
                    Status = "Soh",
                    Description = "Batarya saglik durumu (%).",
                    Register = 0x355,
                    Value = 1,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0007",
                    Message = "Average Module Voltage",
                    Status = "Average Module Voltage",
                    Description = "Batarya paketinin ortalama mod�l voltaji.",
                    Register = 0x356,
                    Value = 0,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0008",
                    Message = "Total Current",
                    Status = "Total Current",
                    Description = "Bataryadan �ekilen (+) veya bataryaya verilen (�) toplam akim.",
                    Register = 0x356,
                    Value = 1,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0009",
                    Message = "Average Cell Temperature",
                    Status = "Average Cell Temperature",
                    Description = "H�crelerin ortalama sicakligi (�C).",
                    Register = 0x356,
                    Value = 2,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // BMS HATA VE UYARI KODLARI (CAN ID 0x359)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-BMS-0001",
                    Message = "Cell/Module Over Voltage Protect",
                    Status = "Cell/Module Over Voltage Protect",
                    Description = "H�cre asiri sarj oldu.",
                    Register = 0x359,
                    Value = 1,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-BMS-0002",
                    Message = "Cell/Module Under Voltage Protect",
                    Status = "Cell/Module Under Voltage Protect",
                    Description = "H�cre/mod�l asiri desarj oldu.",
                    Register = 0x359,
                    Value = 2,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-BMS-0003",
                    Message = "Cell Over Temperature Protect",
                    Status = "Cell Over Temperature Protect",
                    Description = "H�cre asiri isindi.",
                    Register = 0x359,
                    Value = 3,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-BMS-0004",
                    Message = "Cell Under Temperature Protect",
                    Status = "Cell Under Temperature Protect",
                    Description = "H�cre asiri soguk.",
                    Register = 0x359,
                    Value = 4,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-BMS-0005",
                    Message = "Discharge Over Current Protect",
                    Status = "Discharge Over Current Protect",
                    Description = "Desarj akimi �ok y�ksek.",
                    Register = 0x359,
                    Value = 7,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-BMS-0006",
                    Message = "Charge Over Current Protect",
                    Status = "Charge Over Current Protect",
                    Description = "Sarj akimi �ok y�ksek.",
                    Register = 0x359,
                    Value = 8,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ERR-BMS-0007",
                    Message = "System Error",
                    Status = "System Error",
                    Description = "Dahili sistem hatasi.",
                    Register = 0x359,
                    Value = 11,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // BMS ALARM KODLARI
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ALR-BMS-0001",
                    Message = "Cell/Module High Voltage Alarm",
                    Status = "Cell/Module High Voltage Alarm",
                    Description = "H�cre voltaji y�ksek.",
                    Register = 0x359,
                    Value = 16,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ALR-BMS-0002",
                    Message = "Cell/Module Low Voltage Alarm",
                    Status = "Cell/Module Low Voltage Alarm",
                    Description = "H�cre voltaji d�s�k.",
                    Register = 0x359,
                    Value = 17,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ALR-BMS-0003",
                    Message = "Cell High Temperature Alarm",
                    Status = "Cell High Temperature Alarm",
                    Description = "H�cre sicakligi y�ksek.",
                    Register = 0x359,
                    Value = 18,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ALR-BMS-0004",
                    Message = "Cell Low Temperature Alarm",
                    Status = "Cell Low Temperature Alarm",
                    Description = "H�cre sicakligi d�s�k.",
                    Register = 0x359,
                    Value = 19,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ALR-BMS-0005",
                    Message = "Discharge High Current Alarm",
                    Status = "Discharge High Current Alarm",
                    Description = "Desarj akimi y�ksek.",
                    Register = 0x359,
                    Value = 23,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ALR-BMS-0006",
                    Message = "Charge High Current Alarm",
                    Status = "Charge High Current Alarm",
                    Description = "Sarj akimi y�ksek.",
                    Register = 0x359,
                    Value = 24,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "ALR-BMS-0007",
                    Message = "Slave Communication Off-line",
                    Status = "Slave Communication Off-line",
                    Description = "Bir mod�l veya paralel batarya grubu ile iletisim kesildi.",
                    Register = 0x359,
                    Value = 27,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // BMS'DEN GELEN KRITIK KONTROL BAYRAKLARI (CAN ID 0x35C)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0010",
                    Message = "Full Charge Request",
                    Status = "Full Charge Request",
                    Description = "BMS tam sarj isteginde bulundu.",
                    Register = 0x35C,
                    Value = 3,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0011",
                    Message = "Force Charge Request",
                    Status = "Force Charge Request",
                    Description = "Batarya d�s�k kapasitede, derhal sarj edilmeli.",
                    Register = 0x35C,
                    Value = 4,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0012",
                    Message = "Discharge Enable",
                    Status = "Discharge Enable",
                    Description = "1: Desarja izin verilir. 0: Durdurulmali.",
                    Register = 0x35C,
                    Value = 6,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0013",
                    Message = "Charge Enable",
                    Status = "Charge Enable",
                    Description = "1: Sarja izin verilir. 0: Durdurulmali.",
                    Register = 0x35C,
                    Value = 7,
                    Category = "INFO",
                    Version = "1.0",
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
}
