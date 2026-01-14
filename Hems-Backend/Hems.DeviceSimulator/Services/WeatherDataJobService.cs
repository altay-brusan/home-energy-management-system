using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Hems.DeviceSimulator.Services;

public interface IWeatherDataJobService
{
    Task StartWeatherSimulationAsync();
    Task StopWeatherSimulationAsync();
    Task ScheduleWeatherJobAsync(Guid deviceId);
    Task UnscheduleWeatherJobAsync(Guid deviceId);
}

/// <summary>
/// Service for simulating weather data for demo devices.
/// Sends weather data every 30 minutes directly to the database.
/// </summary>
public class WeatherDataJobService : IWeatherDataJobService
{
    private readonly ILogger<WeatherDataJobService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly IRecurringJobManager _recurringJobManager;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Random _random = new();

    // Istanbul weather patterns (demo location)
    private readonly string[] _weatherConditions = { "Sunny", "Partly Cloudy", "Cloudy", "Light Rain", "Clear" };

    public WeatherDataJobService(
        ILogger<WeatherDataJobService> logger,
        IConfiguration configuration,
        IApplicationDbContext context,
        IRecurringJobManager recurringJobManager)
    {
        _logger = logger;
        _configuration = configuration;
        _context = context;
        _recurringJobManager = recurringJobManager;
    }

    public async Task StartWeatherSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Starting Weather Simulation with Hangfire...");

            var devices = await GetTargetDevicesAsync();

            _logger.LogInformation("Found {DeviceCount} devices for Weather simulation", devices.Count);

            foreach (var device in devices)
            {
                await ScheduleWeatherJobAsync(device.Id);
            }

            _logger.LogInformation("All Weather simulation jobs scheduled");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopWeatherSimulationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Stopping Weather Simulation...");

            var devices = await GetTargetDevicesAsync();

            foreach (var device in devices)
            {
                await UnscheduleWeatherJobAsync(device.Id);
            }

            _logger.LogInformation("All Weather simulation jobs stopped");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task ScheduleWeatherJobAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"weather-{deviceId}";
            // Run every 30 minutes
            var cronExpression = _configuration.GetValue<string>("DeviceSimulator:WeatherCronExpression", "*/30 * * * *");

            _recurringJobManager.AddOrUpdate(
                jobId,
                () => SendWeatherDataJob(deviceId),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            _logger.LogInformation("Scheduled Weather job for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling Weather job for device {DeviceId}", deviceId);
        }

        return Task.CompletedTask;
    }

    public Task UnscheduleWeatherJobAsync(Guid deviceId)
    {
        try
        {
            var jobId = $"weather-{deviceId}";
            _recurringJobManager.RemoveIfExists(jobId);

            _logger.LogInformation("Removed Weather job for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Weather job for device {DeviceId}", deviceId);
        }

        return Task.CompletedTask;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendWeatherDataJob(Guid deviceId)
    {
        try
        {
            _logger.LogDebug("Executing Weather job for device {DeviceId}", deviceId);

            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);

            if (device == null)
            {
                _logger.LogWarning("Device {DeviceId} not found, removing job", deviceId);
                await UnscheduleWeatherJobAsync(deviceId);
                return;
            }

            // Generate realistic weather data
            var weatherData = GenerateWeatherData(device.ThingsBoardDeviceId);

            // Save directly to database (weather data doesn't go through API)
            await SaveWeatherDataAsync(weatherData);

            _logger.LogDebug("Weather job completed for device {DeviceId}", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Weather job for device {DeviceId}", deviceId);
            throw;
        }
    }

    private DeviceWeatherData GenerateWeatherData(Guid thingsBoardDeviceId)
    {
        var now = DateTime.UtcNow;
        var hour = now.Hour;
        var month = now.Month;

        // Base temperature based on month (Istanbul climate)
        var baseTemp = month switch
        {
            1 or 2 or 12 => GenerateRandomValue(5, 12),    // Winter
            3 or 4 or 11 => GenerateRandomValue(10, 18),   // Spring/Fall
            5 or 6 or 9 or 10 => GenerateRandomValue(15, 25), // Mild
            7 or 8 => GenerateRandomValue(25, 35),         // Summer
            _ => GenerateRandomValue(15, 25)
        };

        // Temperature variation by hour
        var hourFactor = Math.Sin((hour - 6) * Math.PI / 12);
        var temperature = baseTemp + (hourFactor * 5);

        // Humidity (inverse to temperature generally)
        var humidity = GenerateRandomValue(40, 80) - (hourFactor * 10);
        humidity = Math.Max(30, Math.Min(95, humidity));

        // Solar factor for condition determination
        double solarFactor = 0;
        if (hour >= 6 && hour <= 18)
        {
            solarFactor = Math.Sin((hour - 6) * Math.PI / 12);
        }

        // Weather condition based on solar factor and randomness
        var conditionRandom = _random.NextDouble();
        var condition = solarFactor > 0.7 && conditionRandom > 0.3 ? "Sunny" :
                        solarFactor > 0.4 && conditionRandom > 0.2 ? "Partly Cloudy" :
                        solarFactor > 0.1 ? "Cloudy" :
                        conditionRandom > 0.7 ? "Light Rain" : "Clear";

        // Temperature condition description
        var tempCondition = temperature > 30 ? "Hot" :
                           temperature > 20 ? "Warm" :
                           temperature > 10 ? "Mild" :
                           temperature > 0 ? "Cool" : "Cold";

        // Wind speed
        var windSpeed = GenerateRandomValue(2, 25);
        var windDirection = _random.Next(8) switch
        {
            0 => "N", 1 => "NE", 2 => "E", 3 => "SE",
            4 => "S", 5 => "SW", 6 => "W", _ => "NW"
        };

        // Cloud cover based on condition
        var cloudCover = condition == "Sunny" ? GenerateRandomValue(0, 20) :
                         condition == "Partly Cloudy" ? GenerateRandomValue(30, 60) :
                         condition == "Cloudy" ? GenerateRandomValue(70, 90) :
                         GenerateRandomValue(80, 100);

        // UV Index
        var uvIndex = hour >= 6 && hour <= 18 && condition != "Cloudy"
            ? GenerateRandomValue(2, 10) * solarFactor
            : 0;

        return new DeviceWeatherData
        {
            ThingsBoardDeviceId = thingsBoardDeviceId,
            DeviceLocalTime = now,
            Temperature = (decimal)Math.Round(temperature, 1),
            TemperatureCondition = tempCondition,
            Humidity = (decimal)Math.Round(humidity, 1),
            WindSpeed = (decimal)Math.Round(windSpeed, 1),
            WindDirection = windDirection,
            Pressure = (decimal)Math.Round(GenerateRandomValue(1010, 1025), 1),
            Condition = condition,
            CloudCover = (decimal)Math.Round(cloudCover, 1),
            UvIndex = (decimal)Math.Round(uvIndex, 1)
        };
    }

    private async Task SaveWeatherDataAsync(DeviceWeatherData weatherData)
    {
        try
        {
            _context.DeviceWeatherData.Add(weatherData);
            await _context.SaveChangesAsync(CancellationToken.None);

            _logger.LogDebug("Weather data saved for device {DeviceId}: {Condition}, {Temp}C",
                weatherData.ThingsBoardDeviceId, weatherData.Condition, weatherData.Temperature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving weather data for device {DeviceId}",
                weatherData.ThingsBoardDeviceId);
            throw;
        }
    }

    private async Task<List<Device>> GetTargetDevicesAsync()
    {
        try
        {
            var demoModeOnly = _configuration.GetValue<bool>("DeviceSimulator:DemoModeOnly", false);

            var query = _context.Devices
                .Where(d => !d.IsDeleted && _context.SetupDevices.Any(sd => sd.DeviceId == d.Id));

            if (demoModeOnly)
            {
                query = query.Where(d => d.IsDemoDevice);
                _logger.LogInformation("DemoModeOnly enabled - targeting only demo devices");
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for Weather simulation");
            return new List<Device>();
        }
    }

    private double GenerateRandomValue(double min, double max)
    {
        return min + (_random.NextDouble() * (max - min));
    }
}
