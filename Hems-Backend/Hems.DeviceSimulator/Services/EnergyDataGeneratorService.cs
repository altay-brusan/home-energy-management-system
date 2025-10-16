using System.Text.Json;
using Hems.DeviceSimulator.Models;

namespace Hems.DeviceSimulator.Services;

public interface IEnergyDataGeneratorService
{
    Task<DailyCumulativeEnergyData> GenerateDailyCumulativeDataAsync(Guid deviceId, DateTime timestamp);
    Task<LifetimeEnergyData> GenerateLifetimeDataAsync(Guid deviceId, DateTime timestamp);
}

public class EnergyDataGeneratorService : IEnergyDataGeneratorService
{
    private readonly Random _random = new();
    private readonly ILogger<EnergyDataGeneratorService> _logger;
    private readonly IJsonDataService _jsonDataService;

    public EnergyDataGeneratorService(ILogger<EnergyDataGeneratorService> logger, IJsonDataService jsonDataService)
    {
        _logger = logger;
        _jsonDataService = jsonDataService;
    }

    public async Task<DailyCumulativeEnergyData> GenerateDailyCumulativeDataAsync(Guid deviceId, DateTime timestamp)
    {
        try
        {
            // JSON'dan günlük veri al
            var energyData = await _jsonDataService.GetEnergyDataByTimeAsync(timestamp, false);
            
            if (energyData == null)
            {
                _logger.LogWarning("No energy data found for timestamp {Timestamp}, using random data", timestamp);
                return GenerateRandomDailyData(deviceId, timestamp);
            }

            return new DailyCumulativeEnergyData
            {
                DateTime = timestamp,
                DeviceId = deviceId,
                PvEnergyKWhToday = energyData.PvEnergyKWhToday,
                GridImportEnergyKWhToday = energyData.GridImportEnergyKWhToday,
                GridExportEnergyKWhToday = energyData.GridExportEnergyKWhToday,
                GeneratorEnergyKWhToday = energyData.GeneratorEnergyKWhToday,
                BatteryDischargeEnergyKWhToday = energyData.BatteryDischargeEnergyKWhToday,
                BatteryChargeEnergyKWhToday = energyData.BatteryChargeEnergyKWhToday,
                LoadEnergyKWhToday = energyData.LoadEnergyKWhToday
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily cumulative data for device {DeviceId}", deviceId);
            return GenerateRandomDailyData(deviceId, timestamp);
        }
    }

    public async Task<LifetimeEnergyData> GenerateLifetimeDataAsync(Guid deviceId, DateTime timestamp)
    {
        try
        {
            // JSON'dan lifetime veri al
            var energyData = await _jsonDataService.GetEnergyDataByTimeAsync(timestamp, true);
            
            if (energyData == null)
            {
                _logger.LogWarning("No lifetime energy data found for timestamp {Timestamp}, using random data", timestamp);
                return GenerateRandomLifetimeData(deviceId, timestamp);
            }

            return new LifetimeEnergyData
            {
                DateTime = timestamp.Date, // Gece yarısı
                DeviceId = deviceId,
                PvEnergyKWhTotal = energyData.PvEnergyKWhTotal,
                GridImportEnergyKWhTotal = energyData.GridImportEnergyKWhTotal,
                GridExportEnergyKWhTotal = energyData.GridExportEnergyKWhTotal,
                GeneratorEnergyKWhTotal = energyData.GeneratorEnergyKWhTotal,
                BatteryDischargeEnergyKWhTotal = energyData.BatteryDischargeEnergyKWhTotal,
                BatteryChargeEnergyKWhTotal = energyData.BatteryChargeEnergyKWhTotal,
                LoadEnergyKWhTotal = energyData.LoadEnergyKWhTotal
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lifetime data for device {DeviceId}", deviceId);
            return GenerateRandomLifetimeData(deviceId, timestamp);
        }
    }

    
    
    
    
    
    
    
    
    // Fallback metodları (JSON verisi bulunamazsa)
    private DailyCumulativeEnergyData GenerateRandomDailyData(Guid deviceId, DateTime timestamp)
    {
        return new DailyCumulativeEnergyData
        {
            DateTime = timestamp,
            DeviceId = deviceId,
            PvEnergyKWhToday = _random.NextDouble() * 30 + 5,
            GridImportEnergyKWhToday = _random.NextDouble() * 10 + 2,
            GridExportEnergyKWhToday = _random.NextDouble() * 5 + 1,
            GeneratorEnergyKWhToday = _random.NextDouble() * 2 + 0.1,
            BatteryDischargeEnergyKWhToday = _random.NextDouble() * 8 + 2,
            BatteryChargeEnergyKWhToday = _random.NextDouble() * 8 + 2,
            LoadEnergyKWhToday = _random.NextDouble() * 25 + 5
        };
    }

    private LifetimeEnergyData GenerateRandomLifetimeData(Guid deviceId, DateTime timestamp)
    {
        return new LifetimeEnergyData
        {
            DateTime = timestamp.Date,
            DeviceId = deviceId,
            PvEnergyKWhTotal = _random.NextDouble() * 10000 + 5000,
            GridImportEnergyKWhTotal = _random.NextDouble() * 5000 + 2000,
            GridExportEnergyKWhTotal = _random.NextDouble() * 3000 + 1000,
            GeneratorEnergyKWhTotal = _random.NextDouble() * 1000 + 100,
            BatteryDischargeEnergyKWhTotal = _random.NextDouble() * 4000 + 2000,
            BatteryChargeEnergyKWhTotal = _random.NextDouble() * 4000 + 2000,
            LoadEnergyKWhTotal = _random.NextDouble() * 10000 + 5000
        };
    }
}
