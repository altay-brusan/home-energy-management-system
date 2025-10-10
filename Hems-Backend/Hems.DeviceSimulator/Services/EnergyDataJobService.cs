using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Hems.DeviceSimulator.Services;

public interface IEnergyDataJobService
{
    Task StartEnergyDataCollectionAsync();
    Task StopEnergyDataCollectionAsync();
    Task ScheduleHourlyEnergyDataAsync(Guid deviceId);
    Task ScheduleMidnightLifetimeDataAsync(Guid deviceId);
    Task UnscheduleEnergyDataCollectionAsync(Guid deviceId);
}

public class EnergyDataJobService : IEnergyDataJobService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EnergyDataJobService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IEnergyDataGeneratorService _energyDataGenerator;
    
    private readonly Dictionary<Guid, string> _deviceTokens = new();
    private readonly Dictionary<Guid, DeviceEnergyState> _deviceEnergyStates = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public EnergyDataJobService(
        IHttpClientFactory httpClientFactory,
        ILogger<EnergyDataJobService> logger,
        IConfiguration configuration,
        IApplicationDbContext context,
        IRecurringJobManager recurringJobManager,
        IEnergyDataGeneratorService energyDataGenerator)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _context = context;
        _recurringJobManager = recurringJobManager;
        _energyDataGenerator = energyDataGenerator;
    }

    public async Task StartEnergyDataCollectionAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("🔋 Starting Energy Data Collection with Hangfire...");

            // DB'den tüm device'ları çek
            var devices = await GetDevicesFromDatabaseAsync();
            
            _logger.LogInformation("📡 Found {DeviceCount} devices for energy data collection", devices.Count);
            
            // Her device için energy state'i initialize et
            foreach (var device in devices)
            {
                var energyState = _energyDataGenerator.InitializeDeviceEnergyState(device.Device.Id);
                _deviceEnergyStates[device.Device.Id] = energyState;
                
                // Saatlik ve gece yarısı job'larını schedule et
                await ScheduleHourlyEnergyDataAsync(device.Device.Id);
                await ScheduleMidnightLifetimeDataAsync(device.Device.Id);
            }
            
            _logger.LogInformation("✅ All energy data collection jobs scheduled with Hangfire");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopEnergyDataCollectionAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("🛑 Stopping Energy Data Collection...");

            // DB'den tüm device'ları çek ve job'ları iptal et
            var devices = await GetDevicesFromDatabaseAsync();
            
            foreach (var device in devices)
            {
                await UnscheduleEnergyDataCollectionAsync(device.Device.Id);
            }
            
            _logger.LogInformation("✅ All energy data collection jobs stopped");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task ScheduleHourlyEnergyDataAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"energy-hourly-{deviceId}";
            var cronExpression = "0 0 * * * *";

            _recurringJobManager.AddOrUpdate(
                jobId,
                () => SendHourlyEnergyDataJob(deviceId),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _logger.LogInformation("📅 Scheduled hourly energy data job for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling hourly energy data job for device {DeviceId}", deviceId);
        }
        
        return Task.CompletedTask;
    }

    public Task ScheduleMidnightLifetimeDataAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"energy-lifetime-{deviceId}";
            var cronExpression = "0 0 0 * * *"; // Her gece yarısı (00:00:00)

            _recurringJobManager.AddOrUpdate(
                jobId,
                () => SendMidnightLifetimeDataJob(deviceId),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _logger.LogInformation("📅 Scheduled midnight lifetime data job for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling midnight lifetime data job for device {DeviceId}", deviceId);
        }
        
        return Task.CompletedTask;
    }

    public Task UnscheduleEnergyDataCollectionAsync(Guid deviceId)
    {
        try
        {
            var hourlyJobId = $"energy-hourly-{deviceId}";
            var lifetimeJobId = $"energy-lifetime-{deviceId}";
            
            _recurringJobManager.RemoveIfExists(hourlyJobId);
            _recurringJobManager.RemoveIfExists(lifetimeJobId);
            
            _logger.LogInformation("🗑️ Removed energy data collection jobs for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing energy data collection jobs for device {DeviceId}", deviceId);
        }
        
        return Task.CompletedTask;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendHourlyEnergyDataJob(Guid deviceId)
    {
        try
        {
            _logger.LogDebug("🔄 Executing hourly energy data job for device {DeviceId}", deviceId);

            // Device'ın energy state'ini güncelle
            if (_deviceEnergyStates.TryGetValue(deviceId, out var currentState))
            {
                var timeElapsed = DateTime.UtcNow - currentState.LastUpdate;
                var updatedState = _energyDataGenerator.UpdateDeviceEnergyState(currentState, timeElapsed);
                _deviceEnergyStates[deviceId] = updatedState;

                // Günlük cumulative data oluştur
                var dailyData = _energyDataGenerator.GenerateDailyCumulativeData(deviceId, updatedState);
                
                // Ana sisteme gönder
                await SendEnergyDataAsync(deviceId, dailyData, "DailyCumulativeEnergyData");
            }
            else
            {
                _logger.LogWarning("Device {DeviceId} energy state not found, initializing", deviceId);
                var newState = _energyDataGenerator.InitializeDeviceEnergyState(deviceId);
                _deviceEnergyStates[deviceId] = newState;
                
                var dailyData = _energyDataGenerator.GenerateDailyCumulativeData(deviceId, newState);
                await SendEnergyDataAsync(deviceId, dailyData, "DailyCumulativeEnergyData");
            }

            _logger.LogDebug("✅ Hourly energy data job completed for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in hourly energy data job for device {DeviceId}", deviceId);
            throw; // Hangfire retry mekanizması için
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendMidnightLifetimeDataJob(Guid deviceId)
    {
        try
        {
            _logger.LogDebug("🔄 Executing midnight lifetime data job for device {DeviceId}", deviceId);

            if (_deviceEnergyStates.TryGetValue(deviceId, out var currentState))
            {
                // Ömür boyu değerleri güncelle (günlük değerleri ekle)
                var updatedState = new DeviceEnergyState
                {
                    DeviceId = deviceId,
                    LastUpdate = DateTime.UtcNow,
                    
                    // Günlük değerleri ömür boyu değerlere ekle
                    PvEnergyKWhTotal = currentState.PvEnergyKWhTotal + currentState.PvEnergyKWhToday,
                    GridImportEnergyKWhTotal = currentState.GridImportEnergyKWhTotal + currentState.GridImportEnergyKWhToday,
                    GridExportEnergyKWhTotal = currentState.GridExportEnergyKWhTotal + currentState.GridExportEnergyKWhToday,
                    GeneratorEnergyKWhTotal = currentState.GeneratorEnergyKWhTotal + currentState.GeneratorEnergyKWhToday,
                    BatteryDischargeEnergyKWhTotal = currentState.BatteryDischargeEnergyKWhTotal + currentState.BatteryDischargeEnergyKWhToday,
                    BatteryChargeEnergyKWhTotal = currentState.BatteryChargeEnergyKWhTotal + currentState.BatteryChargeEnergyKWhToday,
                    LoadEnergyKWhTotal = currentState.LoadEnergyKWhTotal + currentState.LoadEnergyKWhToday,
                    
                    // Günlük değerleri sıfırla
                    PvEnergyKWhToday = 0,
                    GridImportEnergyKWhToday = 0,
                    GridExportEnergyKWhToday = 0,
                    GeneratorEnergyKWhToday = 0,
                    BatteryDischargeEnergyKWhToday = 0,
                    BatteryChargeEnergyKWhToday = 0,
                    LoadEnergyKWhToday = 0,
                    
                    // Güç değerleri aynı kalır
                    PvPowerKw = currentState.PvPowerKw,
                    GridImportPowerKw = currentState.GridImportPowerKw,
                    GridExportPowerKw = currentState.GridExportPowerKw,
                    GeneratorPowerKw = currentState.GeneratorPowerKw,
                    BatteryDischargePowerKw = currentState.BatteryDischargePowerKw,
                    BatteryChargePowerKw = currentState.BatteryChargePowerKw,
                    LoadPowerKw = currentState.LoadPowerKw
                };

                _deviceEnergyStates[deviceId] = updatedState;

                // Ömür boyu data oluştur
                var lifetimeData = _energyDataGenerator.GenerateLifetimeData(deviceId, updatedState);
                
                // Ana sisteme gönder
                await SendEnergyDataAsync(deviceId, lifetimeData, "LifetimeEnergyData");
            }

            _logger.LogDebug("✅ Midnight lifetime data job completed for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in midnight lifetime data job for device {DeviceId}", deviceId);
            throw; // Hangfire retry mekanizması için
        }
    }

    private async Task SendEnergyDataAsync(Guid deviceId, DeviceEnergyState energyData, string dataType)
    {
        await SendEnergyDataInternalAsync(deviceId, energyData, dataType);
    }

    private async Task SendEnergyDataAsync(Guid deviceId, DailyCumulativeEnergyData energyData, string dataType)
    {
        // DailyCumulativeEnergyData'yı DeviceEnergyState'e dönüştür
        var deviceState = new DeviceEnergyState
        {
            DeviceId = deviceId,
            LastUpdate = DateTime.UtcNow,
            PvEnergyKWhToday = energyData.PvEnergyKWhToday,
            GridImportEnergyKWhToday = energyData.GridImportEnergyKWhToday,
            GridExportEnergyKWhToday = energyData.GridExportEnergyKWhToday,
            GeneratorEnergyKWhToday = energyData.GeneratorEnergyKWhToday,
            BatteryDischargeEnergyKWhToday = energyData.BatteryDischargeEnergyKWhToday,
            BatteryChargeEnergyKWhToday = energyData.BatteryChargeEnergyKWhToday,
            LoadEnergyKWhToday = energyData.LoadEnergyKWhToday,
            // Ömür boyu değerler için varsayılan değerler
            PvEnergyKWhTotal = 0,
            GridImportEnergyKWhTotal = 0,
            GridExportEnergyKWhTotal = 0,
            GeneratorEnergyKWhTotal = 0,
            BatteryDischargeEnergyKWhTotal = 0,
            BatteryChargeEnergyKWhTotal = 0,
            LoadEnergyKWhTotal = 0,
            // Güç değerleri için varsayılan değerler
            PvPowerKw = 0,
            GridImportPowerKw = 0,
            GridExportPowerKw = 0,
            GeneratorPowerKw = 0,
            BatteryDischargePowerKw = 0,
            BatteryChargePowerKw = 0,
            LoadPowerKw = 0
        };
        
        await SendEnergyDataInternalAsync(deviceId, deviceState, dataType);
    }

    private async Task SendEnergyDataAsync(Guid deviceId, LifetimeEnergyData energyData, string dataType)
    {
        // LifetimeEnergyData'yı DeviceEnergyState'e dönüştür
        var deviceState = new DeviceEnergyState
        {
            DeviceId = deviceId,
            LastUpdate = DateTime.UtcNow,
            PvEnergyKWhTotal = energyData.PvEnergyKWhTotal,
            GridImportEnergyKWhTotal = energyData.GridImportEnergyKWhTotal,
            GridExportEnergyKWhTotal = energyData.GridExportEnergyKWhTotal,
            GeneratorEnergyKWhTotal = energyData.GeneratorEnergyKWhTotal,
            BatteryDischargeEnergyKWhTotal = energyData.BatteryDischargeEnergyKWhTotal,
            BatteryChargeEnergyKWhTotal = energyData.BatteryChargeEnergyKWhTotal,
            LoadEnergyKWhTotal = energyData.LoadEnergyKWhTotal,
            // Günlük değerler için varsayılan değerler
            PvEnergyKWhToday = 0,
            GridImportEnergyKWhToday = 0,
            GridExportEnergyKWhToday = 0,
            GeneratorEnergyKWhToday = 0,
            BatteryDischargeEnergyKWhToday = 0,
            BatteryChargeEnergyKWhToday = 0,
            LoadEnergyKWhToday = 0,
            // Güç değerleri için varsayılan değerler
            PvPowerKw = 0,
            GridImportPowerKw = 0,
            GridExportPowerKw = 0,
            GeneratorPowerKw = 0,
            BatteryDischargePowerKw = 0,
            BatteryChargePowerKw = 0,
            LoadPowerKw = 0
        };
        
        await SendEnergyDataInternalAsync(deviceId, deviceState, dataType);
    }

    private async Task SendEnergyDataInternalAsync(Guid deviceId, DeviceEnergyState energyData, string dataType)
    {
        try
        {
            var token = await GetDeviceTokenAsync(deviceId);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("No token available for device {DeviceId}", deviceId);
                return;
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["HemsBackend:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Yeni endpoint formatında gönder
            var energyDataCommand = new
            {
                DataType = dataType,
                DeviceId = deviceId,
                DataDateTime = DateTime.UtcNow,
                
                // Günlük değerler
                PvEnergyKWhToday = energyData.PvEnergyKWhToday,
                GridImportEnergyKWhToday = energyData.GridImportEnergyKWhToday,
                GridExportEnergyKWhToday = energyData.GridExportEnergyKWhToday,
                GeneratorEnergyKWhToday = energyData.GeneratorEnergyKWhToday,
                BatteryDischargeEnergyKWhToday = energyData.BatteryDischargeEnergyKWhToday,
                BatteryChargeEnergyKWhToday = energyData.BatteryChargeEnergyKWhToday,
                LoadEnergyKWhToday = energyData.LoadEnergyKWhToday,
                
                // Ömür boyu değerler
                PvEnergyKWhTotal = energyData.PvEnergyKWhTotal,
                GridImportEnergyKWhTotal = energyData.GridImportEnergyKWhTotal,
                GridExportEnergyKWhTotal = energyData.GridExportEnergyKWhTotal,
                GeneratorEnergyKWhTotal = energyData.GeneratorEnergyKWhTotal,
                BatteryDischargeEnergyKWhTotal = energyData.BatteryDischargeEnergyKWhTotal,
                BatteryChargeEnergyKWhTotal = energyData.BatteryChargeEnergyKWhTotal,
                LoadEnergyKWhTotal = energyData.LoadEnergyKWhTotal
            };

            var json = JsonSerializer.Serialize(energyDataCommand);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/DeviceData/energy", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("✅ Energy data sent successfully for device {DeviceId} ({DataType})", deviceId, dataType);
            }
            else
            {
                _logger.LogError("❌ Failed to send energy data for device {DeviceId} ({DataType}): {StatusCode}",
                    deviceId, dataType, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending energy data for device {DeviceId} ({DataType})", deviceId, dataType);
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
            
            _logger.LogInformation("📡 Retrieved {DeviceCount} devices for energy data collection", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices from database for energy data collection");
            return new List<DeviceWithComponents>();
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
            // Device'ı bul
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null || string.IsNullOrEmpty(device.SerialNumber))
            {
                _logger.LogError("❌ Device {DeviceId} not found or SerialNumber is empty", deviceId);
                return string.Empty;
            }

            var loginData = new
            {
                SerialNumber = device.SerialNumber
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
                    _deviceTokens[deviceId] = loginResponse.accessToken;
                    _logger.LogInformation("✅ Device {DeviceId} ({SerialNumber}) logged in successfully for energy data",
                        deviceId, device.SerialNumber);
                    return loginResponse.accessToken;
                }
            }
            
            _logger.LogError("❌ Login failed for device {DeviceId} ({SerialNumber}): {StatusCode}",
                deviceId, device.SerialNumber, response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for device {DeviceId} for energy data", deviceId);
            return string.Empty;
        }
    }
}
