using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hems.DeviceSimulator.Services;

public interface IDeviceSimulatorService
{
    Task StartSimulationAsync();
    Task StopSimulationAsync();
    Task<bool> IsRunningAsync();
    Task SendDeviceDataAsync(DeviceWithComponents device, object data);
    Task<(object? Data, string DataType)?> GenerateTestDataAsync(Guid deviceId, Guid componentId);
}

public class DeviceSimulatorService : IDeviceSimulatorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeviceSimulatorService> _logger;
    private readonly IDataGeneratorService _dataGenerator;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    
    private bool _isRunning = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Dictionary<Guid, string> _deviceTokens = new();

    public DeviceSimulatorService(
        IHttpClientFactory httpClientFactory,
        ILogger<DeviceSimulatorService> logger,
        IDataGeneratorService dataGenerator,
        IConfiguration configuration,
        IApplicationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dataGenerator = dataGenerator;
        _configuration = configuration;
        _context = context;
    }

    public async Task StartSimulationAsync()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Simulation is already running!");
            return;
        }

        _logger.LogInformation("🚀 Starting Device Simulation...");
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // DB'den tüm device'ları çek (componentleri ile birlikte)
            var devices = await GetDevicesFromDatabaseAsync();
            
            _logger.LogInformation("📡 Found {DeviceCount} devices in database", devices.Count);
            
            // Her device için ayrı task başlat (background'da çalışsın)
            foreach (var device in devices)
            {
                _ = Task.Run(() => SimulateDeviceAsync(device, _cancellationTokenSource.Token));
            }
            
            _logger.LogInformation("✅ All device simulation tasks started in background");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Simulation stopped by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during simulation");
        }
        finally
        {
            _isRunning = false;
            _cancellationTokenSource?.Dispose();
        }
    }

    public Task StopSimulationAsync()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Simulation is not running!");
            return Task.CompletedTask;
        }

        _logger.LogInformation("🛑 Stopping Device Simulation...");
        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    public Task<bool> IsRunningAsync()
    {
        return Task.FromResult(_isRunning);
    }

    public async Task SendDeviceDataAsync(DeviceWithComponents device, object data)
    {
        try
        {
            var token = await GetDeviceTokenAsync(device);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("No token available for device {DeviceId}", device.Device.Id);
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
                DataType = data.GetType().Name, // Örn: "BatteryData", "InverterData"
                DeviceId = device.Device.Id,
                RequestDate = DateTime.UtcNow,
                Data = JsonSerializer.SerializeToDocument(data)
            };

            var json = JsonSerializer.Serialize(deviceDataCommand);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/DeviceData", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Data sent successfully for device {DeviceId}", device.Device.Id);
            }
            else
            {
                _logger.LogError("❌ Failed to send data for device {DeviceId}: {StatusCode}",
                    device.Device.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data for device {DeviceId}", device.Device.Id);
        }
    }

    public async Task<(object? Data, string DataType)?> GenerateTestDataAsync(Guid deviceId, Guid componentId)
    {
        try
        {
            // Device'ı ve component'ini bul
            var device = await _context.Devices
                .Include(d => d.DeviceComponents)
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                _logger.LogWarning("Device not found: {DeviceId}", deviceId);
                return null;
            }

            var component = device.DeviceComponents
                .FirstOrDefault(c => c.Id == componentId && !c.IsDeleted);

            if (component == null)
            {
                _logger.LogWarning("Component not found: {ComponentId} for device {DeviceId}", componentId, deviceId);
                return null;
            }

            // DeviceComponent'i DeviceComponentInfo'ya dönüştür
            var componentInfo = new DeviceComponentInfo
            {
                Id = component.Id,
                DeviceId = component.DeviceId,
                DeviceType = component.DeviceType,
                Name = component.Name,
                Manufacturer = component.Manufacturer,
                ModelNumber = component.ModelNumber,
                SerialNumber = component.SerialNumber,
                Specifications = component.Specifications
            };

            // Component için veri üret
            var data = _dataGenerator.GenerateDataForComponent(componentInfo);
            var dataType = GetDataTypeFromComponent(component.DeviceType);
            
            _logger.LogInformation("✅ Generated test data for device {DeviceId}, component {ComponentId} ({ComponentType})", 
                deviceId, componentId, component.DeviceType);

            return (data, dataType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test data for device {DeviceId}, component {ComponentId}", deviceId, componentId);
            return null;
        }
    }

    private string GetDataTypeFromComponent(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.Battery => "BatteryData",
            DeviceType.BMS => "BatteryClusterData",
            DeviceType.Invertor => "InverterData",
            DeviceType.MPPS => "MPPSData",
            _ => "GenericData"
        };
    }

    private async Task<List<DeviceWithComponents>> GetDevicesFromDatabaseAsync()
    {
        try
        {
            // Doğrudan DbContext kullanarak device'ları ve component'lerini çek
            // Sadece SetupDevice tablosunda kayıtlı olan device'ları al
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

    private async Task<List<DeviceComponentInfo>> GetDeviceComponentsAsync(Guid deviceId)
    {
        // Bu method'u daha sonra implement edeceğiz
        // Şimdilik boş liste döndürüyoruz
        await Task.CompletedTask;
        return new List<DeviceComponentInfo>();
    }

    private async Task SimulateDeviceAsync(DeviceWithComponents deviceInfo, CancellationToken cancellationToken)
    {
        var device = deviceInfo.Device;
        var components = deviceInfo.Components;
        
        _logger.LogInformation("📡 Starting simulation for device {DeviceId} ({Manufacturer} {Model})", 
            device.Id, device.Manufacturer, device.ModelNumber);

        // Device'ın component'lerine göre veri üret
        var dataTypes = DetermineDataTypes(components);
        
        if (!dataTypes.Any())
        {
            _logger.LogWarning("No data types found for device {DeviceId}", device.Id);
            return;
        }

        var intervalSeconds = _configuration.GetValue<int>($"DeviceSimulator:DeviceIntervals:{device.Id}", 30);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                foreach (var deviceComponent in components)
                {
                    // Her component için ayrı veri üret
                    var data = _dataGenerator.GenerateDataForComponent(deviceComponent);
                    
                    // Ana sisteme gönder
                    await SendDeviceDataAsync(deviceInfo, data);
                }
                
                // Belirtilen süre kadar bekle
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating device {DeviceId}", device.Id);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // Hata durumunda kısa bekle
            }
        }

        _logger.LogInformation("🛑 Stopped simulation for device {DeviceId}", device.Id);
    }

    private List<DeviceType> DetermineDataTypes(List<DeviceComponentInfo> components)
    {
        var dataTypes = new List<DeviceType>();
        
        foreach (var component in components)
        {
            if (!dataTypes.Contains(component.DeviceType))
            {
                dataTypes.Add(component.DeviceType);
            }
        }
        
        return dataTypes;
    }

    private async Task<string> GetDeviceTokenAsync(DeviceWithComponents device)
    {
        if (_deviceTokens.TryGetValue(device.Device.Id, out var token))
        {
            return token;
        }

        // Token yoksa login yap
        return await LoginDeviceAsync(device);
    }

    private async Task<string> LoginDeviceAsync(DeviceWithComponents device)
    {
        try
        {
            // Device'ın SerialNumber'ını al
           
            
            if (device == null || string.IsNullOrEmpty(device.Device.SerialNumber))
            {
                _logger.LogError("❌ Device {DeviceId} not found or SerialNumber is empty", device!.Device.SerialNumber);
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
                    _logger.LogInformation("✅ Device {DeviceId} ({SerialNumber}) logged in successfully",
                        device.Device.Id, device.Device.SerialNumber);
                    return loginResponse.accessToken;
                }
            }
            
            _logger.LogError("❌ Login failed for device {DeviceId} ({SerialNumber}): {StatusCode}",
                device.Device.Id, device.Device.SerialNumber, response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for device {DeviceId}", device.Device.Id);
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

