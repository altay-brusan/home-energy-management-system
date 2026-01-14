using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Hems.DeviceSimulator.Services;

public interface IPowerFlowJobService
{
    Task StartPowerFlowSimulationAsync();
    Task StopPowerFlowSimulationAsync();
    Task SchedulePowerFlowJobAsync(Guid deviceId);
    Task UnschedulePowerFlowJobAsync(Guid deviceId);
}

/// <summary>
/// Service for simulating real-time PowerFlow data.
/// Sends power data every 5 minutes to /api/DeviceData/PowerFlow endpoint.
/// </summary>
public class PowerFlowJobService : IPowerFlowJobService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PowerFlowJobService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly IRecurringJobManager _recurringJobManager;

    private readonly Dictionary<Guid, string> _deviceTokens = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Random _random = new();

    public PowerFlowJobService(
        IHttpClientFactory httpClientFactory,
        ILogger<PowerFlowJobService> logger,
        IConfiguration configuration,
        IApplicationDbContext context,
        IRecurringJobManager recurringJobManager)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _context = context;
        _recurringJobManager = recurringJobManager;
    }

    public async Task StartPowerFlowSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Starting PowerFlow Simulation with Hangfire...");

            var devices = await GetTargetDevicesAsync();

            _logger.LogInformation("Found {DeviceCount} devices for PowerFlow simulation", devices.Count);

            foreach (var device in devices)
            {
                await SchedulePowerFlowJobAsync(device.Id);
            }

            _logger.LogInformation("All PowerFlow simulation jobs scheduled");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopPowerFlowSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Stopping PowerFlow Simulation...");

            var devices = await GetTargetDevicesAsync();

            foreach (var device in devices)
            {
                await UnschedulePowerFlowJobAsync(device.Id);
            }

            _logger.LogInformation("All PowerFlow simulation jobs stopped");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task SchedulePowerFlowJobAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"powerflow-{deviceId}";
            var cronExpression = _configuration.GetValue<string>("DeviceSimulator:PowerFlowCronExpression", "*/5 * * * *");

            _recurringJobManager.AddOrUpdate(
                jobId,
                () => SendPowerFlowDataJob(deviceId),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _logger.LogInformation("Scheduled PowerFlow job for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling PowerFlow job for device {DeviceId}", deviceId);
        }

        return Task.CompletedTask;
    }

    public Task UnschedulePowerFlowJobAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"powerflow-{deviceId}";
            _recurringJobManager.RemoveIfExists(jobId);

            _logger.LogInformation("Removed PowerFlow job for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing PowerFlow job for device {DeviceId}", deviceId);
        }

        return Task.CompletedTask;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendPowerFlowDataJob(Guid deviceId)
    {
        try
        {
            _logger.LogDebug("Executing PowerFlow job for device {DeviceId}", deviceId);

            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                _logger.LogWarning("Device {DeviceId} not found, removing job", deviceId);
                await UnschedulePowerFlowJobAsync(deviceId);
                return;
            }

            // Generate realistic PowerFlow data based on time of day
            var powerFlowData = GeneratePowerFlowData(device.ThingsBoardDeviceId);

            await SendPowerFlowToBackendAsync(device, powerFlowData);

            _logger.LogDebug("PowerFlow job completed for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PowerFlow job for device {DeviceId}", deviceId);
            throw;
        }
    }

    private object GeneratePowerFlowData(Guid thingsBoardDeviceId)
    {
        var now = DateTime.UtcNow;
        var hour = now.Hour;

        // Energy Balance Equation:
        // PV + GridImport + BatteryDischarge = Load + GridExport + BatteryCharge
        // Or using sign convention:
        // PV + Grid + Battery = Load
        // Where: Grid positive = import, negative = export
        //        Battery positive = discharge, negative = charge

        double pvPower = 0;
        double gridPower = 0;
        double batteryPower = 0;
        double generatorPower = 0;

        // Base load varies by time of day (higher in evening, lower at night)
        double baseLoad = hour switch
        {
            >= 0 and < 6 => GenerateRandomValue(300, 800),      // Night: low
            >= 6 and < 9 => GenerateRandomValue(800, 1500),     // Morning: medium
            >= 9 and < 17 => GenerateRandomValue(500, 1200),    // Day: medium-low
            >= 17 and < 22 => GenerateRandomValue(1500, 3000),  // Evening: high
            _ => GenerateRandomValue(500, 1000)                  // Late night: low
        };
        double loadPower = baseLoad;

        // PV production based on time (simplified sun curve for Istanbul ~41°N latitude)
        if (hour >= 6 && hour <= 19)
        {
            // Peak at 12:00-13:00
            var hourFromNoon = Math.Abs(12.5 - hour);
            var peakFactor = Math.Max(0, 1.0 - (hourFromNoon / 7.0));

            // Max PV power 5kW system, with weather variation
            var maxPvPower = GenerateRandomValue(4000, 5500);
            pvPower = maxPvPower * peakFactor * peakFactor; // Squared for more realistic curve
            pvPower = Math.Max(0, pvPower);
        }

        // Calculate energy balance
        var netPower = pvPower - loadPower;

        if (netPower >= 0)
        {
            // Excess PV power: charge battery first, then export to grid
            var excessPower = netPower;

            // Battery charging (max 3kW charge rate, only if not full - simulated)
            var batteryChargeCapacity = GenerateRandomValue(0, 3000); // Available charge capacity
            var batteryCharge = Math.Min(excessPower * 0.7, batteryChargeCapacity);
            batteryPower = -batteryCharge; // Negative = charging

            // Remaining excess goes to grid (export = negative)
            var gridExport = excessPower - batteryCharge;
            gridPower = -gridExport;
        }
        else
        {
            // Deficit: need more power than PV provides
            var deficit = Math.Abs(netPower);

            // Battery discharging first (max 2.5kW discharge rate)
            var batteryDischargeCapacity = GenerateRandomValue(0, 2500); // Available discharge capacity
            var batteryDischarge = Math.Min(deficit * 0.6, batteryDischargeCapacity);
            batteryPower = batteryDischarge; // Positive = discharging

            // Remaining deficit from grid (import = positive)
            var gridImport = deficit - batteryDischarge;
            gridPower = gridImport;
        }

        // Calculate self-consumption metrics
        var selfConsumption = Math.Min(pvPower, loadPower); // PV power used directly
        var selfConsumptionRate = pvPower > 0 ? (selfConsumption / pvPower) * 100 : 0;
        var autarky = loadPower > 0 ? ((pvPower + Math.Max(0, batteryPower)) / loadPower) * 100 : 0;
        autarky = Math.Min(100, autarky); // Cap at 100%

        // Verify energy balance: PV + Grid + Battery = Load (within rounding tolerance)
        var inputPower = pvPower + gridPower + batteryPower;
        var balanceError = Math.Abs(inputPower - loadPower);
        if (balanceError > 1) // More than 1W error
        {
            _logger.LogWarning("Energy balance error: {Error}W (PV:{PV} + Grid:{Grid} + Battery:{Battery} ≠ Load:{Load})",
                Math.Round(balanceError, 1), Math.Round(pvPower, 1), Math.Round(gridPower, 1),
                Math.Round(batteryPower, 1), Math.Round(loadPower, 1));
        }

        _logger.LogDebug("PowerFlow: PV={PV}W, Grid={Grid}W, Battery={Battery}W, Load={Load}W, SelfConsumption={SC}%",
            Math.Round(pvPower, 0), Math.Round(gridPower, 0), Math.Round(batteryPower, 0),
            Math.Round(loadPower, 0), Math.Round(selfConsumptionRate, 1));

        return new
        {
            deviceId = thingsBoardDeviceId.ToString(),
            createdBy = "Simulator", // Identify data source as simulator
            data = new
            {
                timestampUtc = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                version = 1,
                pv = new
                {
                    totalPowerW = Math.Round(pvPower, 1),
                    mppt = new[]
                    {
                        new { id = 1, powerW = Math.Round(pvPower * 0.52, 1) }, // Slight imbalance for realism
                        new { id = 2, powerW = Math.Round(pvPower * 0.48, 1) }
                    }
                },
                grid = new
                {
                    totalPowerW = Math.Round(gridPower, 1),
                    importPowerW = Math.Round(Math.Max(0, gridPower), 1),
                    exportPowerW = Math.Round(Math.Abs(Math.Min(0, gridPower)), 1)
                },
                load = new
                {
                    totalPowerW = Math.Round(loadPower, 1)
                },
                battery = new
                {
                    powerW = Math.Round(batteryPower, 1),
                    chargePowerW = Math.Round(Math.Abs(Math.Min(0, batteryPower)), 1),
                    dischargePowerW = Math.Round(Math.Max(0, batteryPower), 1),
                    soc = GenerateRandomValue(20, 95) // State of charge percentage
                },
                generator = new
                {
                    totalPowerW = Math.Round(generatorPower, 1)
                },
                selfConsumption = new
                {
                    ratePercent = Math.Round(selfConsumptionRate, 1),
                    autarkyPercent = Math.Round(autarky, 1),
                    directUsageW = Math.Round(selfConsumption, 1)
                }
            }
        };
    }

    private async Task SendPowerFlowToBackendAsync(Hems_Backend.Domain.Entities.Device device, object powerFlowData)
    {
        try
        {
            var token = await GetDeviceTokenAsync(device);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("No token available for device {DeviceId}", device.Id);
                return;
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["HemsBackend:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);

            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(powerFlowData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/DeviceData/PowerFlow", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("PowerFlow data sent successfully for device {DeviceId}", device.Id);
            }
            else
            {
                _logger.LogError("Failed to send PowerFlow data for device {DeviceId}: {StatusCode}",
                    device.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PowerFlow data for device {DeviceId}", device.Id);
        }
    }

    private async Task<List<Hems_Backend.Domain.Entities.Device>> GetTargetDevicesAsync()
    {
        try
        {
            var demoModeOnly = _configuration.GetValue<bool>("DeviceSimulator:DemoModeOnly", false);

            var query = _context.Devices
                .Where(d => !d.IsDeleted && _context.SetupDevices.Any(sd => sd.DeviceId == d.Id));

            if (demoModeOnly)
            {
                // Only target demo devices
                query = query.Where(d => d.IsDemoDevice);
                _logger.LogInformation("DemoModeOnly enabled - targeting only demo devices");
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for PowerFlow simulation");
            return new List<Hems_Backend.Domain.Entities.Device>();
        }
    }

    private async Task<string> GetDeviceTokenAsync(Hems_Backend.Domain.Entities.Device device)
    {
        if (_deviceTokens.TryGetValue(device.Id, out var token))
        {
            return token;
        }

        return await LoginDeviceAsync(device);
    }

    private async Task<string> LoginDeviceAsync(Hems_Backend.Domain.Entities.Device device)
    {
        try
        {
            if (string.IsNullOrEmpty(device.SerialNumber))
            {
                _logger.LogError("Device {DeviceId} has no serial number", device.Id);
                return string.Empty;
            }

            var loginData = new { SerialNumber = device.SerialNumber };
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["HemsBackend:BaseUrl"] ?? "https://localhost:7001";
            httpClient.BaseAddress = new Uri(baseUrl);

            var response = await httpClient.PostAsync("/api/Devices/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);

                if (!string.IsNullOrEmpty(loginResponse?.accessToken))
                {
                    _deviceTokens[device.Id] = loginResponse.accessToken;
                    _logger.LogInformation("Device {DeviceId} ({SerialNumber}) logged in successfully",
                        device.Id, device.SerialNumber);
                    return loginResponse.accessToken;
                }
            }

            _logger.LogError("Login failed for device {DeviceId}: {StatusCode}",
                device.Id, response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for device {DeviceId}", device.Id);
            return string.Empty;
        }
    }

    private double GenerateRandomValue(double min, double max)
    {
        return min + (_random.NextDouble() * (max - min));
    }
}
