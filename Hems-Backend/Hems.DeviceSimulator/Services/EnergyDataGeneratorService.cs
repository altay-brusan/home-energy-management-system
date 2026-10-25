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
            _logger.LogError(ex, "Error generating daily cumulative data for device {ThingsBoardDeviceId}", deviceId);
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
            _logger.LogError(ex, "Error generating lifetime data for device {ThingsBoardDeviceId}", deviceId);
            return GenerateRandomLifetimeData(deviceId, timestamp);
        }
    }

    
    
    
    
    
    
    
    
    // Fallback methods (when JSON data not available)
    // Energy Balance: PV + GridImport + BatteryDischarge + Generator = Load + GridExport + BatteryCharge
    private DailyCumulativeEnergyData GenerateRandomDailyData(Guid deviceId, DateTime timestamp)
    {
        // Start with realistic base values for a 5kW PV system
        var pvEnergy = GenerateRandomValue(15, 35);        // 15-35 kWh/day from solar
        var loadEnergy = GenerateRandomValue(12, 28);      // 12-28 kWh/day consumption
        var generatorEnergy = 0.0;                          // Generator rarely used

        // Calculate energy flows based on PV vs Load
        double gridImport, gridExport, batteryCharge, batteryDischarge;

        var netEnergy = pvEnergy - loadEnergy;

        if (netEnergy >= 0)
        {
            // PV produces more than load - excess goes to battery and grid
            gridImport = GenerateRandomValue(0.5, 2);  // Small import for night/morning
            var excessEnergy = netEnergy + gridImport;

            // Split excess between battery charge and grid export
            var batteryChargeRatio = GenerateRandomValue(0.4, 0.7);
            batteryCharge = excessEnergy * batteryChargeRatio;
            batteryCharge = Math.Min(batteryCharge, 10); // Max 10 kWh battery

            // Battery discharge for evening use
            batteryDischarge = batteryCharge * GenerateRandomValue(0.7, 0.95); // Use 70-95% of charged energy

            // Grid export is the remaining excess
            gridExport = excessEnergy - batteryCharge + batteryDischarge - gridImport;
            gridExport = Math.Max(0, gridExport);
        }
        else
        {
            // Load exceeds PV - need battery and grid
            var deficit = Math.Abs(netEnergy);
            gridExport = GenerateRandomValue(0, 1);  // Small export during peak PV

            // Battery covers part of deficit
            var batteryRatio = GenerateRandomValue(0.3, 0.6);
            batteryDischarge = deficit * batteryRatio;
            batteryDischarge = Math.Min(batteryDischarge, 8); // Max 8 kWh discharge

            // Grid import covers the rest
            gridImport = deficit - batteryDischarge + gridExport;

            // Some battery charging during peak PV hours
            batteryCharge = batteryDischarge * GenerateRandomValue(0.8, 1.1);
        }

        // Verify energy balance: PV + GridImport + BatteryDischarge + Generator = Load + GridExport + BatteryCharge
        var inputs = pvEnergy + gridImport + batteryDischarge + generatorEnergy;
        var outputs = loadEnergy + gridExport + batteryCharge;
        var balanceError = Math.Abs(inputs - outputs);

        // Adjust load to ensure perfect balance
        if (balanceError > 0.1)
        {
            loadEnergy = inputs - gridExport - batteryCharge;
            _logger.LogDebug("Adjusted load to {Load} kWh for energy balance", Math.Round(loadEnergy, 2));
        }

        return new DailyCumulativeEnergyData
        {
            DateTime = timestamp,
            DeviceId = deviceId,
            PvEnergyKWhToday = Math.Round(pvEnergy, 2),
            GridImportEnergyKWhToday = Math.Round(gridImport, 2),
            GridExportEnergyKWhToday = Math.Round(gridExport, 2),
            GeneratorEnergyKWhToday = Math.Round(generatorEnergy, 2),
            BatteryDischargeEnergyKWhToday = Math.Round(batteryDischarge, 2),
            BatteryChargeEnergyKWhToday = Math.Round(batteryCharge, 2),
            LoadEnergyKWhToday = Math.Round(loadEnergy, 2)
        };
    }

    private LifetimeEnergyData GenerateRandomLifetimeData(Guid deviceId, DateTime timestamp)
    {
        // Simulate ~2 years of operation (730 days)
        var operationDays = GenerateRandomValue(600, 800);

        // Average daily values scaled up
        var avgDailyPv = GenerateRandomValue(18, 28);
        var avgDailyLoad = GenerateRandomValue(15, 25);

        var pvEnergyTotal = avgDailyPv * operationDays;
        var loadEnergyTotal = avgDailyLoad * operationDays;
        var generatorEnergyTotal = GenerateRandomValue(50, 200); // Rarely used

        // Calculate balanced lifetime values
        double gridImportTotal, gridExportTotal, batteryChargeTotal, batteryDischargeTotal;

        var netEnergyTotal = pvEnergyTotal - loadEnergyTotal;

        if (netEnergyTotal >= 0)
        {
            // Net producer
            gridImportTotal = operationDays * GenerateRandomValue(1, 3);
            var totalExcess = netEnergyTotal + gridImportTotal;

            batteryChargeTotal = totalExcess * GenerateRandomValue(0.4, 0.6);
            batteryDischargeTotal = batteryChargeTotal * GenerateRandomValue(0.85, 0.95); // 85-95% round-trip

            gridExportTotal = totalExcess - batteryChargeTotal + batteryDischargeTotal;
        }
        else
        {
            // Net consumer
            var deficit = Math.Abs(netEnergyTotal);
            gridExportTotal = operationDays * GenerateRandomValue(0.5, 2);

            batteryDischargeTotal = deficit * GenerateRandomValue(0.3, 0.5);
            batteryChargeTotal = batteryDischargeTotal * GenerateRandomValue(1.05, 1.15); // Charge slightly more than discharge

            gridImportTotal = deficit - batteryDischargeTotal + batteryChargeTotal + gridExportTotal;
        }

        // Verify and adjust for balance
        var inputs = pvEnergyTotal + gridImportTotal + batteryDischargeTotal + generatorEnergyTotal;
        var outputs = loadEnergyTotal + gridExportTotal + batteryChargeTotal;
        var balanceError = Math.Abs(inputs - outputs);

        if (balanceError > 1)
        {
            loadEnergyTotal = inputs - gridExportTotal - batteryChargeTotal;
        }

        return new LifetimeEnergyData
        {
            DateTime = timestamp.Date,
            DeviceId = deviceId,
            PvEnergyKWhTotal = Math.Round(pvEnergyTotal, 1),
            GridImportEnergyKWhTotal = Math.Round(gridImportTotal, 1),
            GridExportEnergyKWhTotal = Math.Round(gridExportTotal, 1),
            GeneratorEnergyKWhTotal = Math.Round(generatorEnergyTotal, 1),
            BatteryDischargeEnergyKWhTotal = Math.Round(batteryDischargeTotal, 1),
            BatteryChargeEnergyKWhTotal = Math.Round(batteryChargeTotal, 1),
            LoadEnergyKWhTotal = Math.Round(loadEnergyTotal, 1)
        };
    }

    private double GenerateRandomValue(double min, double max)
    {
        return min + (_random.NextDouble() * (max - min));
    }
}
