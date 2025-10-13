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

        // Seed Device Notification Types
        if (!_context.DeviceNotificationTypes.Any())
        {
            var notificationTypes = new List<DeviceNotificationType>
            {
                // INVERTER - INFO / DURUM BİLGİLERİ
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0001",
                    Message = "Standby",
                    Status = "Standby",
                    Description = "Inverter hazırda beklemede. PV veya Batarya bağlı değil ya da \"enable\" edilmemiş.",
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
                    Description = "Inverter açılış öncesi iç testleri yapıyor. Normal başlangıç aşaması.",
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
                    Description = "İdeal durum. Inverter sorunsuz çalışıyor, enerji dönüşümü yapıyor.",
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
                    Description = "Inverter bir uyarı (warning) durumunda ama çalışmaya devam ediyor. EMS, Reg.553–554 aralığını izlemeye devam etmeli.",
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
                    Description = "Kritik durum. Bir hata oluştu, inverter durdu veya durmak üzere. EMS, Reg.555–558 aralığındaki fault kelimelerini incelemeli.",
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
                    Description = "Inverter açılıyor ve şebekeye senkronize oluyor. Geçici durum.",
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
                    Description = "Inverter \"ON\" komutunda. Çalışmaya hazır.",
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
                    Description = "Inverter'ın çalışma modu: 0 → Watt, 1 → PF, 2 → VAr.",
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
                    Description = "AC coupling özelliği aktif. AC kaynak üzerinden batarya şarj edilebilir.",
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
                    Description = "\"Battery First\" (10) veya \"Load First\" (11) modlarını temsil eder.",
                    Register = 141,
                    Value = 10,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // RÖLE DURUMLARI (AC Relay Status – Register 552)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-INV-0009",
                    Message = "INV Relay",
                    Status = "INV Relay",
                    Description = "İnverter çıkışı devrede. Yük beslemesi aktif.",
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
                    Description = "Şebeke (Grid) devrede, senkronize durumda.",
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
                    Description = "Jeneratör devrede ve inverter tarafından kullanılabilir durumda.",
                    Register = 552,
                    Value = 3,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // INVERTER FAULT / WARNING LİSTESİ
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "WAR-INV-0002",
                    Message = "Fan Warning",
                    Status = "Fan Warning",
                    Description = "Fan hızı düşük / arızalı. Inverter sıcaklığı artabilir.",
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
                    Description = "Faz sırası hatalı. Grid bağlantısı yapılamaz veya kesilir.",
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
                    Description = "Şebeke voltajı düşük limitlere yaklaşıyor.",
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
                    Description = "Şebeke voltajı yüksek limitlere yaklaşıyor.",
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
                    Description = "BMS ile haberleşme kesildi veya batarya bağlantısı koptu.",
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
                    Description = "Paralel çalışan inverterler arasında haberleşme kalitesi düşük.",
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
                    Description = "PV giriş voltajı çok yüksek.",
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
                    Description = "Şebeke voltajı izin verilen maksimum değeri aştı.",
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
                    Description = "Şebeke voltajı izin verilen minimum değerin altına düştü.",
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
                    Description = "Şebeke frekansı izin verilen bandın dışında.",
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
                    Description = "DC Bus voltajı tehlikeli seviyede. Acil shutdown.",
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
                    Description = "DC Bus voltajı çok düşük.",
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
                    Description = "Inverter aşırı ısındı.",
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
                    Description = "Inverter çıkışında kısa devre tespit edildi.",
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
                    Description = "Batarya şarj voltajı çok yüksek.",
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
                    Description = "Batarya voltajı çok düşük.",
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
                    Description = "Batarya akımı limiti aştı.",
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
                    Description = "BMS ile sürekli bir haberleşme hatası.",
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
                    Description = "Ana röle açılamadı veya kapanamadı.",
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
                    Description = "PV veya batarya tarafında toprak kaçak akımı.",
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
                    Description = "Şebeke bağlantı rölesi arızalı.",
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
                    Description = "Ana kontrolör (MCU) ile güç katmanı arasında haberleşme kesildi.",
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
                    Description = "EEPROM okunamıyor veya yazılamıyor.",
                    Register = 558,
                    Value = 2,
                    Category = "ERROR",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // ÇALIŞMA LİMİTLERİ (Operation Limits – CAN ID 0x351)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0001",
                    Message = "Charge Voltage Limit",
                    Status = "Charge Voltage Limit",
                    Description = "Bataryanın şu anda kabul edebileceği maksimum şarj voltajı.",
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
                    Description = "Bataryanın kabul edebileceği maksimum şarj akımı.",
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
                    Description = "Bataryanın sağlayabileceği maksimum deşarj akımı.",
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
                    Description = "Bataryanın izin verilen minimum deşarj voltajı.",
                    Register = 0x351,
                    Value = 3,
                    Category = "INFO",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // SİSTEM SAĞLIK ÖLÇÜMLERİ (CAN ID 0x355, 0x356)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0005",
                    Message = "SOC",
                    Status = "SOC",
                    Description = "Batarya şarj seviyesi (%).",
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
                    Message = "SOH",
                    Status = "SOH",
                    Description = "Batarya sağlık durumu (%).",
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
                    Description = "Batarya paketinin ortalama modül voltajı.",
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
                    Description = "Bataryadan çekilen (+) veya bataryaya verilen (–) toplam akım.",
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
                    Description = "Hücrelerin ortalama sıcaklığı (°C).",
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
                    Description = "Hücre aşırı şarj oldu.",
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
                    Description = "Hücre/modül aşırı deşarj oldu.",
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
                    Description = "Hücre aşırı ısındı.",
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
                    Description = "Hücre aşırı soğuk.",
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
                    Description = "Deşarj akımı çok yüksek.",
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
                    Description = "Şarj akımı çok yüksek.",
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
                    Description = "Dahili sistem hatası.",
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
                    Description = "Hücre voltajı yüksek.",
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
                    Description = "Hücre voltajı düşük.",
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
                    Description = "Hücre sıcaklığı yüksek.",
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
                    Description = "Hücre sıcaklığı düşük.",
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
                    Description = "Deşarj akımı yüksek.",
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
                    Description = "Şarj akımı yüksek.",
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
                    Description = "Bir modül veya paralel batarya grubu ile iletişim kesildi.",
                    Register = 0x359,
                    Value = 27,
                    Category = "ALARM",
                    Version = "1.0",
                    IsActive = true,
                    Created = DateTimeOffset.UtcNow,
                    CreatedBy = administrator.Id
                },

                // BMS'DEN GELEN KRİTİK KONTROL BAYRAKLARI (CAN ID 0x35C)
                new DeviceNotificationType
                {
                    Id = Guid.NewGuid(),
                    Code = "INF-BMS-0010",
                    Message = "Full Charge Request",
                    Status = "Full Charge Request",
                    Description = "BMS tam şarj isteğinde bulundu.",
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
                    Description = "Batarya düşük kapasitede, derhal şarj edilmeli.",
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
                    Description = "1: Deşarja izin verilir. 0: Durdurulmalı.",
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
                    Description = "1: Şarja izin verilir. 0: Durdurulmalı.",
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
    }
}
