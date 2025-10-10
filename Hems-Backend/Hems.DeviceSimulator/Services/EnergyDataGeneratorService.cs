using System.Text.Json;

namespace Hems.DeviceSimulator.Services;

public interface IEnergyDataGeneratorService
{
    DailyCumulativeEnergyData GenerateDailyCumulativeData(Guid deviceId, DeviceEnergyState currentState);
    LifetimeEnergyData GenerateLifetimeData(Guid deviceId, DeviceEnergyState currentState);
    DeviceEnergyState InitializeDeviceEnergyState(Guid deviceId);
    DeviceEnergyState UpdateDeviceEnergyState(DeviceEnergyState currentState, TimeSpan timeElapsed);
}

public class EnergyDataGeneratorService : IEnergyDataGeneratorService
{
    private readonly Random _random = new();
    private readonly ILogger<EnergyDataGeneratorService> _logger;

    public EnergyDataGeneratorService(ILogger<EnergyDataGeneratorService> logger)
    {
        _logger = logger;
    }

    public DailyCumulativeEnergyData GenerateDailyCumulativeData(Guid deviceId, DeviceEnergyState currentState)
    {
        var now = DateTime.UtcNow;
        
        return new DailyCumulativeEnergyData
        {
            DateTime = now,
            DeviceId = deviceId,
            PvEnergyKWhToday = Math.Round(currentState.PvEnergyKWhToday, 1),
            GridImportEnergyKWhToday = Math.Round(currentState.GridImportEnergyKWhToday, 1),
            GridExportEnergyKWhToday = Math.Round(currentState.GridExportEnergyKWhToday, 1),
            GeneratorEnergyKWhToday = Math.Round(currentState.GeneratorEnergyKWhToday, 1),
            BatteryDischargeEnergyKWhToday = Math.Round(currentState.BatteryDischargeEnergyKWhToday, 1),
            BatteryChargeEnergyKWhToday = Math.Round(currentState.BatteryChargeEnergyKWhToday, 1),
            LoadEnergyKWhToday = Math.Round(currentState.LoadEnergyKWhToday, 1)
        };
    }

    public LifetimeEnergyData GenerateLifetimeData(Guid deviceId, DeviceEnergyState currentState)
    {
        var midnight = DateTime.UtcNow.Date;
        
        return new LifetimeEnergyData
        {
            DateTime = midnight,
            DeviceId = deviceId,
            PvEnergyKWhTotal = Math.Round(currentState.PvEnergyKWhTotal, 1),
            GridImportEnergyKWhTotal = Math.Round(currentState.GridImportEnergyKWhTotal, 1),
            GridExportEnergyKWhTotal = Math.Round(currentState.GridExportEnergyKWhTotal, 1),
            GeneratorEnergyKWhTotal = Math.Round(currentState.GeneratorEnergyKWhTotal, 1),
            BatteryDischargeEnergyKWhTotal = Math.Round(currentState.BatteryDischargeEnergyKWhTotal, 1),
            BatteryChargeEnergyKWhTotal = Math.Round(currentState.BatteryChargeEnergyKWhTotal, 1),
            LoadEnergyKWhTotal = Math.Round(currentState.LoadEnergyKWhTotal, 1)
        };
    }

    public DeviceEnergyState InitializeDeviceEnergyState(Guid deviceId)
    {
        var now = DateTime.UtcNow;
        
        // Ömür boyu değerler için gerçekçi başlangıç değerleri
        var lifetimePv = _random.NextDouble() * 10000 + 5000; // 5-15 MWh
        var lifetimeGridImport = _random.NextDouble() * 5000 + 2000; // 2-7 MWh
        var lifetimeGridExport = _random.NextDouble() * 3000 + 1000; // 1-4 MWh
        var lifetimeGenerator = _random.NextDouble() * 1000 + 100; // 0.1-1.1 MWh
        var lifetimeBatteryDischarge = _random.NextDouble() * 4000 + 2000; // 2-6 MWh
        var lifetimeBatteryCharge = lifetimeBatteryDischarge + _random.NextDouble() * 500; // Biraz daha fazla
        var lifetimeLoad = lifetimePv + lifetimeGridImport + lifetimeGenerator - lifetimeGridExport - lifetimeBatteryCharge + _random.NextDouble() * 1000;

        return new DeviceEnergyState
        {
            DeviceId = deviceId,
            LastUpdate = now,
            
            // Günlük değerler için başlangıç değerleri
            PvEnergyKWhToday = _random.NextDouble() * 10 + 5, // 5-15 kWh
            GridImportEnergyKWhToday = _random.NextDouble() * 5 + 2, // 2-7 kWh
            GridExportEnergyKWhToday = _random.NextDouble() * 3 + 1, // 1-4 kWh
            GeneratorEnergyKWhToday = _random.NextDouble() * 1 + 0.1, // 0.1-1.1 kWh
            BatteryDischargeEnergyKWhToday = _random.NextDouble() * 4 + 2, // 2-6 kWh
            BatteryChargeEnergyKWhToday = _random.NextDouble() * 4 + 2, // 2-6 kWh
            LoadEnergyKWhToday = _random.NextDouble() * 10 + 5, // 5-15 kWh
            
            // Ömür boyu değerler
            PvEnergyKWhTotal = lifetimePv,
            GridImportEnergyKWhTotal = lifetimeGridImport,
            GridExportEnergyKWhTotal = lifetimeGridExport,
            GeneratorEnergyKWhTotal = lifetimeGenerator,
            BatteryDischargeEnergyKWhTotal = lifetimeBatteryDischarge,
            BatteryChargeEnergyKWhTotal = lifetimeBatteryCharge,
            LoadEnergyKWhTotal = lifetimeLoad,
            
            // Güç değerleri (kW)
            PvPowerKw = GenerateRealisticPower(0, 10), // PV güç
            GridImportPowerKw = GenerateRealisticPower(0, 5), // Şebeke import
            GridExportPowerKw = GenerateRealisticPower(0, 8), // Şebeke export
            GeneratorPowerKw = GenerateRealisticPower(0, 3), // Generator
            BatteryDischargePowerKw = GenerateRealisticPower(0, 5), // Batarya deşarj
            BatteryChargePowerKw = GenerateRealisticPower(0, 5), // Batarya şarj
            LoadPowerKw = GenerateRealisticPower(2, 15) // Yük
        };
    }

    public DeviceEnergyState UpdateDeviceEnergyState(DeviceEnergyState currentState, TimeSpan timeElapsed)
    {
        var hoursElapsed = timeElapsed.TotalHours;
        
        // Debug log
        Console.WriteLine($"🔄 UpdateDeviceEnergyState - HoursElapsed: {hoursElapsed:F4}, CurrentToday: {currentState.PvEnergyKWhToday:F2}");
        var updatedState = new DeviceEnergyState
        {
            DeviceId = currentState.DeviceId,
            LastUpdate = DateTime.UtcNow,
            
            // Ömür boyu değerler aynı kalır (sadece gece yarısı güncellenir)
            PvEnergyKWhTotal = currentState.PvEnergyKWhTotal,
            GridImportEnergyKWhTotal = currentState.GridImportEnergyKWhTotal,
            GridExportEnergyKWhTotal = currentState.GridExportEnergyKWhTotal,
            GeneratorEnergyKWhTotal = currentState.GeneratorEnergyKWhTotal,
            BatteryDischargeEnergyKWhTotal = currentState.BatteryDischargeEnergyKWhTotal,
            BatteryChargeEnergyKWhTotal = currentState.BatteryChargeEnergyKWhTotal,
            LoadEnergyKWhTotal = currentState.LoadEnergyKWhTotal
        };

        // Güç değerlerini güncelle (gerçekçi varyasyonlar)
        updatedState.PvPowerKw = GenerateRealisticPower(0, 10);
        updatedState.GridImportPowerKw = GenerateRealisticPower(0, 5);
        updatedState.GridExportPowerKw = GenerateRealisticPower(0, 8);
        updatedState.GeneratorPowerKw = GenerateRealisticPower(0, 3);
        updatedState.BatteryDischargePowerKw = GenerateRealisticPower(0, 5);
        updatedState.BatteryChargePowerKw = GenerateRealisticPower(0, 5);
        updatedState.LoadPowerKw = GenerateRealisticPower(2, 15);

        // Günlük birikimli değerleri güncelle
        updatedState.PvEnergyKWhToday = currentState.PvEnergyKWhToday + (updatedState.PvPowerKw * hoursElapsed);
        updatedState.GridImportEnergyKWhToday = currentState.GridImportEnergyKWhToday + (updatedState.GridImportPowerKw * hoursElapsed);
        updatedState.GridExportEnergyKWhToday = currentState.GridExportEnergyKWhToday + (updatedState.GridExportPowerKw * hoursElapsed);
        updatedState.GeneratorEnergyKWhToday = currentState.GeneratorEnergyKWhToday + (updatedState.GeneratorPowerKw * hoursElapsed);
        updatedState.BatteryDischargeEnergyKWhToday = currentState.BatteryDischargeEnergyKWhToday + (updatedState.BatteryDischargePowerKw * hoursElapsed);
        updatedState.BatteryChargeEnergyKWhToday = currentState.BatteryChargeEnergyKWhToday + (updatedState.BatteryChargePowerKw * hoursElapsed);
        updatedState.LoadEnergyKWhToday = currentState.LoadEnergyKWhToday + (updatedState.LoadPowerKw * hoursElapsed);
        
        // Debug log
        Console.WriteLine($"📊 Updated Today Values - PV: {updatedState.PvEnergyKWhToday:F2}, Power: {updatedState.PvPowerKw:F2}, Hours: {hoursElapsed:F4}");

        return updatedState;
    }

    private double GenerateRealisticPower(double minKw, double maxKw)
    {
        // Gerçekçi güç değerleri için normal dağılım kullan
        var mean = (minKw + maxKw) / 2;
        var stdDev = (maxKw - minKw) / 6; // 99.7% değerler aralıkta
        
        var value = GenerateNormalDistribution(mean, stdDev);
        return Math.Max(minKw, Math.Min(maxKw, value));
    }

    private double GenerateNormalDistribution(double mean, double stdDev)
    {
        // Box-Muller transformasyonu
        var u1 = _random.NextDouble();
        var u2 = _random.NextDouble();
        var z0 = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        return mean + stdDev * z0;
    }
}
