using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Hems.DeviceSimulator.Services;

public interface IDeviceSimulationJobService
{
    Task StartSimulationAsync();
    Task StopSimulationAsync();
    Task<bool> IsRunningAsync();
    Task ScheduleDeviceSimulationAsync(Guid deviceId, int intervalSeconds);
    Task UnscheduleDeviceSimulationAsync(Guid deviceId);
    Task SendDeviceDataAsync(DeviceWithComponents device, object data);
}

public class DeviceSimulationJobService : IDeviceSimulationJobService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeviceSimulationJobService> _logger;
    private readonly IDataGeneratorService _dataGenerator;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly IRecurringJobManager _recurringJobManager;
    
    private readonly Dictionary<Guid, string> _deviceTokens = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public DeviceSimulationJobService(
        IHttpClientFactory httpClientFactory,
        ILogger<DeviceSimulationJobService> logger,
        IDataGeneratorService dataGenerator,
        IConfiguration configuration,
        IApplicationDbContext context,
        IRecurringJobManager recurringJobManager)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dataGenerator = dataGenerator;
        _configuration = configuration;
        _context = context;
        _recurringJobManager = recurringJobManager;
    }

    public async Task StartSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("🚀 Starting Device Simulation with Hangfire...");

            // DB'den tüm device'ları çek
            var devices = await GetDevicesFromDatabaseAsync();
            
            _logger.LogInformation("📡 Found {DeviceCount} devices in database", devices.Count);
            
            // Her device için recurring job oluştur
            foreach (var device in devices)
            {
                var intervalSeconds = _configuration.GetValue<int>("DeviceSimulator:DefaultIntervalSeconds", 300);
                await ScheduleDeviceSimulationAsync(device.Device.Id, intervalSeconds);
            }
            
            _logger.LogInformation("✅ All device simulation jobs scheduled with Hangfire");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("🛑 Stopping Device Simulation...");

            // DB'den tüm device'ları çek ve job'ları iptal et
            var devices = await GetDevicesFromDatabaseAsync();
            
            foreach (var device in devices)
            {
                await UnscheduleDeviceSimulationAsync(device.Device.Id);
            }
            
            _logger.LogInformation("✅ All device simulation jobs stopped");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsRunningAsync()
    {
        try
        {
            // Database'den device'ları çek ve job'ların varlığını kontrol et
            var devices = await GetDevicesFromDatabaseAsync();
            
            // En az bir device varsa ve job'lar çalışıyorsa true döner
            // Gerçek implementasyonda Hangfire API'sini kullanarak job durumunu kontrol edebiliriz
            return devices.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking simulation status");
            return false;
        }
    }

    public Task ScheduleDeviceSimulationAsync(Guid deviceId, int intervalSeconds)
    {
        try
        {
            var jobId = $"device-simulation-{deviceId}";
            
            // Configuration'dan cron expression al
            var cronExpression = _configuration.GetValue<string>("DeviceSimulator:CronExpression", "0 */5 * * * *");

            _recurringJobManager.AddOrUpdate(
                jobId,
                () => SimulateDeviceJob(deviceId),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _logger.LogInformation("📅 Scheduled recurring job for device {ThingsBoardDeviceId} every 5 minutes", 
                deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling job for device {ThingsBoardDeviceId}", deviceId);
        }
        
        return Task.CompletedTask;
    }

    public Task UnscheduleDeviceSimulationAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"device-simulation-{deviceId}";
            _recurringJobManager.RemoveIfExists(jobId);
            
            _logger.LogInformation("🗑️ Removed recurring job for device {ThingsBoardDeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing job for device {ThingsBoardDeviceId}", deviceId);
        }
        
        return Task.CompletedTask;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SimulateDeviceJob(Guid deviceId)
    {
        try
        {
            _logger.LogDebug("🔄 Executing simulation job for device {ThingsBoardDeviceId}", deviceId);

            // Device'ı ve component'lerini çek
            var device = await GetDeviceWithComponentsAsync(deviceId);
            if (device == null)
            {
                _logger.LogWarning("Device {ThingsBoardDeviceId} not found, removing job", deviceId);
                await UnscheduleDeviceSimulationAsync(deviceId);
                return;
            }

            // Her component için veri üret ve gönder
            foreach (var component in device.Components)
            {
                // Inverter için JSON'dan, diğerleri için random veri üret
            var data = component.DeviceType switch
            {
                DeviceType.Invertor or DeviceType.BMS or DeviceType.Battery => await _dataGenerator.GenerateDataFromJsonAsync(component, DateTime.UtcNow),
                _ => _dataGenerator.GenerateDataForComponent(component)
            };
                    
                await SendDeviceDataAsync(device, data);
            }

            _logger.LogDebug("✅ Simulation job completed for device {ThingsBoardDeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in simulation job for device {ThingsBoardDeviceId}", deviceId);
            throw; // Hangfire retry mekanizması için
        }
    }

    public async Task SendDeviceDataAsync(DeviceWithComponents device, object data)
    {
        try
        {
            var token = await GetDeviceTokenAsync(device);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("No token available for device {ThingsBoardDeviceId}", device.Device.Id);
                return;
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["HemsBackend:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // CreateDeviceDataCommand formatında gönder
            var deviceDataCommand = new
            {
                DataType = data.GetType().Name,
                DeviceId = device.Device.Id,
                DeviceLocalTime = DateTime.UtcNow,
                Data = JsonSerializer.SerializeToDocument(data),
                CreatedBy = "Simulator" // Identify data source as simulator
            };

            var json = JsonSerializer.Serialize(deviceDataCommand);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/DeviceData", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("✅ Data sent successfully for device {ThingsBoardDeviceId}", device.Device.Id);
            }
            else
            {
                _logger.LogError("❌ Failed to send data for device {ThingsBoardDeviceId}: {StatusCode}",
                    device.Device.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data for device {ThingsBoardDeviceId}", device.Device.Id);
        }
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
            
            _logger.LogInformation("📡 Retrieved {DeviceCount} devices with components from database", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices from database");
            return new List<DeviceWithComponents>();
        }
    }

    private async Task<DeviceWithComponents?> GetDeviceWithComponentsAsync(Guid deviceId)
    {
        try
        {
            var device = await _context.Devices
                .Include(d => d.DeviceComponents)
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
                return null;

            return new DeviceWithComponents
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
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device {ThingsBoardDeviceId} from database", deviceId);
            return null;
        }
    }

    private async Task<string> GetDeviceTokenAsync(DeviceWithComponents device)
    {
        if (_deviceTokens.TryGetValue(device.Device.Id, out var token))
        {
            return token;
        }

        return await LoginDeviceAsync(device);
    }

    private async Task<string> LoginDeviceAsync(DeviceWithComponents device)
    {
        try
        {
            if (device == null || string.IsNullOrEmpty(device.Device.SerialNumber))
            {
                _logger.LogError("❌ Device {ThingsBoardDeviceId} not found or SerialNumber is empty", device!.Device.SerialNumber);
                return string.Empty;
            }

            var loginData = new
            {
                SerialNumber = device.Device.SerialNumber
            };

            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["HemsBackend:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);
            
            var response = await httpClient.PostAsync("/api/devices/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);
                
                if (!string.IsNullOrEmpty(loginResponse?.accessToken))
                {
                    _deviceTokens[device.Device.Id] = loginResponse.accessToken;
                    _logger.LogInformation("✅ Device {ThingsBoardDeviceId} ({SerialNumber}) logged in successfully",
                        device.Device.Id, device.Device.SerialNumber);
                    return loginResponse.accessToken;
                }
            }
            
            _logger.LogError("❌ Login failed for device {ThingsBoardDeviceId} ({SerialNumber}): {StatusCode}",
                device.Device.Id, device.Device.SerialNumber, response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for device {ThingsBoardDeviceId}", device.Device.Id);
            return string.Empty;
        }
    }
}

// Helper Classes
public class DeviceWithComponents
{
    public MockDeviceDto Device { get; set; } = null!;
    public List<DeviceComponentInfo> Components { get; set; } = new();
}

public class MockDeviceDto
{
    public Guid Id { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public class DeviceComponentInfo
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DeviceType DeviceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public JsonDocument Specifications { get; set; } = JsonDocument.Parse("{}");
}

public class LoginResponse
{
    public string accessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
