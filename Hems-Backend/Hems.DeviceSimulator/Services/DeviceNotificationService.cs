using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Hems.DeviceSimulator.Services;

public class DeviceNotificationType
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
}

public class DeviceNotificationRequest
{
    public Guid DeviceId { get; set; }
    public string NotificationTypeCode { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string AdditionalData { get; set; } = string.Empty;
}

public interface IDeviceNotificationJobService
{
    Task StartDeviceNotificationSimulationAsync();
    Task StopDeviceNotificationSimulationAsync();
    Task ScheduleDeviceNotificationAsync(Guid deviceId, int intervalSeconds);
    Task UnscheduleDeviceNotificationAsync(Guid deviceId);
}

public class DeviceNotificationJobService : IDeviceNotificationJobService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeviceNotificationJobService> _logger;
    private readonly IApplicationDbContext _context;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IConfiguration _configuration;
    private readonly List<DeviceNotificationType> _notificationTypes = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<Guid, string> _deviceTokens = new();

    public DeviceNotificationJobService(
        IHttpClientFactory httpClientFactory,
        ILogger<DeviceNotificationJobService> logger,
        IApplicationDbContext context,
        IRecurringJobManager recurringJobManager,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _context = context;
        _recurringJobManager = recurringJobManager;
        _configuration = configuration;
        InitializeNotificationTypesAsync().Wait();
    }

    private async Task InitializeNotificationTypesAsync()
    {
        try
        {
                   var dbNotificationTypes = await _context.DeviceNotificationTypes
                       .Where(nt => nt.IsActive)
                       .Select(nt => new DeviceNotificationType
                       {
                           Code = nt.Code,
                           Message = nt.Message,
                           Description = nt.Description,
                           Category = nt.Category,
                           Severity = nt.Severity,
                           Version = nt.Version,
                           Manufacturer = nt.Manufacturer
                       })
                       .ToListAsync();

            _notificationTypes.AddRange(dbNotificationTypes);
            
            _logger.LogInformation("Loaded {Count} notification types from database", _notificationTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notification types from database, using fallback data");
            
            // Fallback: Hardcoded data if database fails
            InitializeFallbackNotificationTypes();
        }
    }

    private void InitializeFallbackNotificationTypes()
    {
        // Generic fallback notification types
        _notificationTypes.AddRange(new[]
        {
            new DeviceNotificationType { Code = "INV-ALM-GENERIC", Message = "Inverter Alarm", Description = "A generic inverter alarm was detected", Category = "Inverter", Severity = "ALARM", Version = "1.0", Manufacturer = "Generic" },
            new DeviceNotificationType { Code = "INV-WAR-GENERIC", Message = "Inverter Warning", Description = "A generic inverter warning was detected", Category = "Inverter", Severity = "WARNING", Version = "1.0", Manufacturer = "Generic" },
            new DeviceNotificationType { Code = "INV-ERR-GENERIC", Message = "Inverter Error", Description = "A generic inverter error was detected", Category = "Inverter", Severity = "ERROR", Version = "1.0", Manufacturer = "Generic" },
            new DeviceNotificationType { Code = "BMS-ALM-GENERIC", Message = "BMS Alarm", Description = "A generic BMS alarm was detected", Category = "Battery", Severity = "ALARM", Version = "1.0", Manufacturer = "Generic" },
            new DeviceNotificationType { Code = "BMS-WAR-GENERIC", Message = "BMS Warning", Description = "A generic BMS warning was detected", Category = "Battery", Severity = "WARNING", Version = "1.0", Manufacturer = "Generic" },
            new DeviceNotificationType { Code = "BMS-ERR-GENERIC", Message = "BMS Error", Description = "A generic BMS error was detected", Category = "Battery", Severity = "ERROR", Version = "1.0", Manufacturer = "Generic" }
        });
    }

    public async Task StartDeviceNotificationSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("🔔 Starting Device Notification Simulation with Hangfire...");

            // DB'den tüm device'ları çek
            var devices = await GetDevicesFromDatabaseAsync();
            
            _logger.LogInformation("📡 Found {DeviceCount} devices for notification simulation", devices.Count);
            
            // Her device için notification job'ını schedule et
            foreach (var device in devices)
            {
                // Her device için aynı interval (5 dakika)
                var intervalSeconds = _configuration.GetValue<int>("DeviceSimulator:DefaultIntervalSeconds", 300);
                await ScheduleDeviceNotificationAsync(device.Device.Id, intervalSeconds);
            }
            
            _logger.LogInformation("✅ All device notification jobs scheduled with Hangfire");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopDeviceNotificationSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("🛑 Stopping Device Notification Simulation...");

            // DB'den tüm device'ları çek ve job'ları iptal et
            var devices = await GetDevicesFromDatabaseAsync();
            
            foreach (var device in devices)
            {
                await UnscheduleDeviceNotificationAsync(device.Device.Id);
            }
            
            _logger.LogInformation("✅ All device notification jobs stopped");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task ScheduleDeviceNotificationAsync(Guid deviceId, int intervalSeconds)
    {
        try
        {
            var jobId = $"device-notification-{deviceId}";
            
            // Configuration'dan cron expression al
            var cronExpression = _configuration.GetValue<string>("DeviceSimulator:CronExpression", "0 */5 * * * *");
            
            _recurringJobManager.AddOrUpdate(
                jobId,
                () => SendDeviceNotificationJob(deviceId),
                cronExpression);
            
            _logger.LogInformation("📅 Scheduled notification job for device {ThingsBoardDeviceId} every 5 minutes", 
                deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification job for device {ThingsBoardDeviceId}", deviceId);
        }
        
        return Task.CompletedTask;
    }

    public Task UnscheduleDeviceNotificationAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"device-notification-{deviceId}";
            
            _recurringJobManager.RemoveIfExists(jobId);
            
            _logger.LogInformation("🗑️ Removed notification job for device {ThingsBoardDeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing notification job for device {ThingsBoardDeviceId}", deviceId);
        }
        
        return Task.CompletedTask;
    }

    private async Task<List<DeviceWithComponents>> GetDevicesFromDatabaseAsync()
    {
        try
        {
            var devices = await _context.Devices
                .Include(d => d.DeviceComponents)
                .Where(d => !d.IsDeleted && 
                           _context.SetupDevices.Any(sd => sd.DeviceId == d.Id))
                .OrderBy(d => d.Manufacturer)
                .ThenBy(d => d.ModelNumber)
                .ToListAsync();
            
            var result = devices.Select(device => new DeviceWithComponents
            {
                Device = new MockDeviceDto
                {
                    Id = device.Id,
                    Manufacturer = device.Manufacturer,
                    ModelNumber = device.ModelNumber,
                    SerialNumber = device.SerialNumber
                },
                Components = device.DeviceComponents
                    .Where(c => !c.IsDeleted)
                    .Select(component => new DeviceComponentInfo
                    {
                        Id = component.Id,
                        DeviceId = component.DeviceId,
                        DeviceType = component.DeviceType,
                        Name = component.Name,
                        Manufacturer = component.Manufacturer,
                        ModelNumber = component.ModelNumber,
                        SerialNumber = component.SerialNumber,
                        Specifications = component.Specifications
                    }).ToList()
            }).ToList();
            
            _logger.LogInformation("📡 Retrieved {DeviceCount} devices for notification simulation", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices from database for notification simulation");
            return new List<DeviceWithComponents>();
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendDeviceNotificationJob(Guid deviceId)
    {
        try
        {
            _logger.LogDebug("🔔 Executing notification job for device {ThingsBoardDeviceId}", deviceId);

            // Device bilgilerini al
            var device = await _context.Devices
                .Include(d => d.SetupDevice!)
                .ThenInclude(sd => sd.Setup!)
                .ThenInclude(s => s.Site!)
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device?.SetupDevice?.Setup?.Site == null)
            {
                _logger.LogWarning("Device {ThingsBoardDeviceId} not found or not assigned to any site", deviceId);
                return;
            }

            var siteId = device.SetupDevice.Setup.Site.Id;
            var deviceName = $"{device.Manufacturer} {device.ModelNumber}";

            // Device token'ını al
            var token = await GetDeviceTokenAsync(deviceId);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("No token available for device {ThingsBoardDeviceId}", deviceId);
                return;
            }

            // Rastgele notification type seç
            var notificationType = _notificationTypes[Random.Shared.Next(_notificationTypes.Count)];
            
            var request = new DeviceNotificationRequest
            {
                DeviceId = deviceId,
                NotificationTypeCode = notificationType.Code,
                Timestamp = DateTimeOffset.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { DeviceName = deviceName, RandomValue = Random.Shared.Next(1, 100) })
            };

            // HTTP Client oluştur ve token ile gönder
            using var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["HemsBackend:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/DeviceNotifications", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("📤 Sent notification {NotificationCode} for device {ThingsBoardDeviceId} in site {SiteId}", 
                    notificationType.Code, deviceId, siteId);
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to send notification for device {ThingsBoardDeviceId}. Status: {StatusCode}", 
                    deviceId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending notification for device {ThingsBoardDeviceId}", deviceId);
            throw; // Hangfire retry için throw et
        }
    }

    private async Task<string> GetDeviceTokenAsync(Guid deviceId)
    {
        if (_deviceTokens.TryGetValue(deviceId, out var token))
        {
            return token;
        }

        return await LoginDeviceAsync(deviceId);
    }

    private async Task<string> LoginDeviceAsync(Guid deviceId)
    {
        try
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null || string.IsNullOrEmpty(device.SerialNumber))
            {
                _logger.LogError("Device {ThingsBoardDeviceId} not found or has no serial number", deviceId);
                return string.Empty;
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["HemsBackend:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);

            var loginRequest = new
            {
                SerialNumber = device.SerialNumber
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/Devices/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
                
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.accessToken))
                {
                    _deviceTokens[deviceId] = loginResponse.accessToken;
                    _logger.LogInformation("🔑 Device {ThingsBoardDeviceId} logged in successfully", deviceId);
                    return loginResponse.accessToken;
                }
            }

            _logger.LogError("❌ Failed to login device {ThingsBoardDeviceId}: {StatusCode}", deviceId, response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in device {ThingsBoardDeviceId}", deviceId);
            return string.Empty;
        }
    }

    public void Dispose()
    {
        // Hangfire job'ları temizle
        try
        {
            var devices = _context.Devices
                .Where(d => !d.IsDeleted)
                .Select(d => d.Id)
                .ToList();

            foreach (var deviceId in devices)
            {
                _recurringJobManager.RemoveIfExists($"device-notification-{deviceId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up notification jobs during dispose");
        }
    }
}