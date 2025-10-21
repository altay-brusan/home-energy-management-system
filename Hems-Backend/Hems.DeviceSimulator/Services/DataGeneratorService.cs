using System.Text.Json;
using Hems_Backend.Domain.Enums;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Hems.DeviceSimulator.Models;

namespace Hems.DeviceSimulator.Services;

public interface IDataGeneratorService
{
    object GenerateDataForComponent(DeviceComponentInfo component);
    Task<object> GenerateLiveDataForComponentAsync(Guid deviceId, Guid componentId);
    Task<object> GenerateDataFromJsonAsync(DeviceComponentInfo component, DateTime timestamp);
    T GenerateRandomValue<T>(T min, T max) where T : IComparable<T>;
}

public class DataGeneratorService : IDataGeneratorService
{
    private readonly Random _random = new();
    private readonly ILogger<DataGeneratorService> _logger;
    private readonly IApplicationDbContext _context;
    private readonly IJsonDataService _jsonDataService;

    public DataGeneratorService(
        ILogger<DataGeneratorService> logger, 
        IApplicationDbContext context,
        IJsonDataService jsonDataService)
    {
        _logger = logger;
        _context = context;
        _jsonDataService = jsonDataService;
    }

    public async Task<object> GenerateLiveDataForComponentAsync(Guid deviceId, Guid componentId)
    {
        _logger.LogInformation("🔍 Fetching component from database: DeviceId={DeviceId}, ComponentId={ComponentId}", 
            deviceId, componentId);

        // Component'i database'den çek
        var component = await _context.DeviceComponents
            .FirstOrDefaultAsync(dc => dc.Id == componentId && 
                                      dc.DeviceId == deviceId && 
                                      !dc.IsDeleted);

        if (component == null)
        {
            _logger.LogWarning("❌ Component not found: DeviceId={DeviceId}, ComponentId={ComponentId}", 
                deviceId, componentId);
            throw new ArgumentException($"Component not found: DeviceId={deviceId}, ComponentId={componentId}");
        }

        _logger.LogInformation("✅ Component found: Type={DeviceType}, SerialNumber={SerialNumber}", 
            component.DeviceType, component.SerialNumber);

        var timestamp = DateTime.UtcNow;

        // Component bilgilerini hazırla
        var componentInfo = new DeviceComponentInfo
        {
            Id = componentId,
            DeviceId = deviceId,
            DeviceType = component.DeviceType,
            SerialNumber = component.SerialNumber,
            ModelNumber = component.ModelNumber
        };

        // Live data oluştur - JSON'dan veri al
        return await GenerateDataFromJsonAsync(componentInfo, timestamp);
    }


   

    public object GenerateDataForComponent(DeviceComponentInfo component)
    {
        var timestamp = DateTime.UtcNow;
        
        return component.DeviceType switch
        {
            DeviceType.Battery => GenerateBatteryData(component, timestamp),
            DeviceType.BMS => GenerateBatteryClusterData(component, timestamp),
            DeviceType.Invertor => GenerateInverterData(component, timestamp),
            DeviceType.MPPS => GenerateMPPSData(component, timestamp),
            _ => GenerateGenericData(component.DeviceType, timestamp)
        };
    }

    public async Task<object> GenerateDataFromJsonAsync(DeviceComponentInfo component, DateTime timestamp)
    {
        try
        {
            return component.DeviceType switch
            {
                DeviceType.Invertor => await GenerateInverterDataFromJsonAsync(component, timestamp),
                DeviceType.BMS => await GenerateBMSDataFromJsonAsync(component, timestamp),
                DeviceType.Battery => await GenerateBatteryDataFromJsonAsync(component, timestamp),
                _ => GenerateDataForComponent(component) // Diğer tipler için random veri
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data from JSON for component {ComponentId}", component.Id);
            return GenerateDataForComponent(component);
        }
    }

    private async Task<object> GenerateInverterDataFromJsonAsync(DeviceComponentInfo component, DateTime timestamp)
    {
        try
        {
            // JSON'dan inverter verisi al
            var jsonInverterData = await _jsonDataService.GetInverterDataByTimeAsync(timestamp);
            
            if (jsonInverterData == null)
            {
                _logger.LogWarning("No inverter JSON data found for timestamp {Timestamp}, using random data", timestamp);
                return GenerateInverterData(component, timestamp);
            }

            // JSON verisini InverterData formatında dönüştür
            return new InverterData
            {
                LastInverterStartupTime = jsonInverterData.LastInverterStartupTime,
                LastInverterShutdownTime = jsonInverterData.LastInverterShutdownTime,
                ActivePower = jsonInverterData.ActivePower,
                ReactivePower = jsonInverterData.ReactivePower,
                PowerFactor = jsonInverterData.PowerFactor,
                Phases = jsonInverterData.Phases,
                InsulationResistance = jsonInverterData.InsulationResistance,
                DeviceSerialNumber = component.SerialNumber ?? jsonInverterData.DeviceSerialNumber,
                FirmwareVersion = jsonInverterData.FirmwareVersion,
                DateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inverter data from JSON for component {ComponentId}", component.Id);
            return GenerateInverterData(component, timestamp);
        }
    }

    private async Task<object> GenerateBMSDataFromJsonAsync(DeviceComponentInfo component, DateTime timestamp)
    {
        try
        {
            // JSON'dan BMS verisi al
            var jsonBMSData = await _jsonDataService.GetBMSDataByTimeAsync(timestamp);
            
            if (jsonBMSData == null)
            {
                _logger.LogWarning("No BMS JSON data found for timestamp {Timestamp}, using random data", timestamp);
                return GenerateBatteryData(component, timestamp);
            }

            // JSON verisini BMSData formatında dönüştür
            return new Models.BatteryClusterData
            {
                VoltagePack = jsonBMSData.VoltagePack,
                CurrentPack = jsonBMSData.CurrentPack,
                MaxVoltageOfBatteryCluster = jsonBMSData.MaxVoltageOfBatteryCluster,
                MinVoltageOfBatteryCluster = jsonBMSData.MinVoltageOfBatteryCluster,
                SOC = jsonBMSData.SOC,
                TotalChargeCapacity = jsonBMSData.TotalChargeCapacity,
                TotalDischargeCapacity = jsonBMSData.TotalDischargeCapacity,
                DateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating BMS data from JSON for component {ComponentId}", component.Id);
            return GenerateBatteryData(component, timestamp);
        }
    }

    private async Task<object> GenerateBatteryDataFromJsonAsync(DeviceComponentInfo component, DateTime timestamp)
    {
        try
        {
            // JSON'dan battery verisi al
            var jsonBatteryData = await _jsonDataService.GetBatteryDataByTimeAsync(timestamp);
            
            if (jsonBatteryData == null)
            {
                _logger.LogWarning("No battery JSON data found for timestamp {Timestamp}, using random data", timestamp);
                return GenerateBatteryData(component, timestamp);
            }

            // JSON verisini BatteryData formatında dönüştür
            return new BatteryData
            {
                BatteryPackVoltage = jsonBatteryData.BatteryPackVoltage,
                BatteryCurrent = jsonBatteryData.BatteryCurrent,
                SOH = jsonBatteryData.SOH.FirstOrDefault(), // İlk SOH değerini al
                SOCCumulative = jsonBatteryData.SOCCumulative, // SOC Cumulative array'ini direkt al
                SOC = jsonBatteryData.SOC.Select(cell => new BatteryCell
                {
                    CellNumber = cell.CellNumber,
                    CellVoltage = cell.CellVoltage,
                    CellTemperature = cell.CellTemperature
                }).ToList(),
                TotalChargeCapacity = jsonBatteryData.TotalChargeCapacity,
                TotalDischargeAmount = jsonBatteryData.TotalDischargeAmount,
                MaxCellVoltage = jsonBatteryData.MaxCellVoltage,
                MinCellVoltage = jsonBatteryData.MinCellVoltage,
                MaxCellTemperature = jsonBatteryData.MaxCellTemperature,
                MinCellTemperature = jsonBatteryData.MinCellTemperature,
                DeviceModel = component.ModelNumber ?? jsonBatteryData.DeviceModel,
                DeviceSerialNumber = component.SerialNumber?? jsonBatteryData.DeviceSerialNumber,
                FirmwareVersion = jsonBatteryData.FirmwareVersion,
                DateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating battery data from JSON for component {ComponentId}", component.Id);
            return GenerateBatteryData(component, timestamp);
        }
    }

    private BatteryData GenerateBatteryData(DeviceComponentInfo component, DateTime timestamp)
    {
        var cellCount = _random.Next(8, 16);
        
        var cells = new List<BatteryCell>();
        
        for (int i = 1; i <= cellCount; i++)
        {
            cells.Add(new BatteryCell
            {
                CellNumber = i,
                CellVoltage = GenerateRandomValue(3.2, 4.2), // Lityum iyon batarya voltaj aralığı
                CellTemperature = GenerateRandomValue(15.0, 45.0) // Normal çalışma sıcaklığı
            });
        }

        var maxVoltage = cells.Max(c => c.CellVoltage);
        var minVoltage = cells.Min(c => c.CellVoltage);
        var maxTemp = cells.Max(c => c.CellTemperature);
        var minTemp = cells.Min(c => c.CellTemperature);

        return new BatteryData
        {
            BatteryPackVoltage = GenerateRandomValue(48.0, 60.0), // Batarya paketi voltajı
            BatteryCurrent = GenerateRandomValue(-50.0, 50.0), // Şarj/deşarj akımı
            SOH = GenerateRandomValue(80.0, 100.0), // State of Health (%)
            SOC = cells,
            TotalChargeCapacity = "1.38 KW",
            TotalDischargeAmount = GenerateRandomValue(0.0, 1000.0),
            MaxCellVoltage = maxVoltage,
            MinCellVoltage = minVoltage,
            MaxCellTemperature = maxTemp,
            MinCellTemperature = minTemp,
            DeviceModel = component.ModelNumber ?? "HEMS-BAT-001",
            DeviceSerialNumber = component.SerialNumber ?? $"BAT{_random.Next(100000, 999999)}",
            FirmwareVersion = $"v1.1",
            DateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private BatteryClusterData GenerateBatteryClusterData(DeviceComponentInfo component, DateTime timestamp)
    {
        // BMS (Battery Management System) - Batarya kümesi verilerini üret
        return new BatteryClusterData
        {
            VoltagePack = GenerateRandomValue(400.0, 500.0),
            CurrentPack = GenerateRandomValue(-100.0, 100.0),
            MaxVoltageOfBatteryCluster = GenerateRandomValue(4.2, 4.5),
            MinVoltageOfBatteryCluster = GenerateRandomValue(3.0, 3.5),
            SOC = GenerateRandomValue(20.0, 100.0),
            TotalChargeCapacity = GenerateRandomValue(50.0, 100.0),
            TotalDischargeCapacity = GenerateRandomValue(45.0, 95.0),
            DateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private InverterData GenerateInverterData(DeviceComponentInfo component, DateTime timestamp)
    {
        var phases = new List<InverterPhase>
        {
            new() { PhaseId = "A", Voltage = GenerateRandomValue(220.0, 240.0), Current = GenerateRandomValue(5.0, 25.0) },
            new() { PhaseId = "B", Voltage = GenerateRandomValue(220.0, 240.0), Current = GenerateRandomValue(5.0, 25.0) },
            new() { PhaseId = "C", Voltage = GenerateRandomValue(220.0, 240.0), Current = GenerateRandomValue(5.0, 25.0) }
        };

        return new InverterData
        {
            LastInverterStartupTime = timestamp.AddMinutes(-_random.Next(30, 480)).ToString("yyyy-MM-dd HH:mm:ss"),
            LastInverterShutdownTime = timestamp.AddMinutes(-_random.Next(5, 30)).ToString("yyyy-MM-dd HH:mm:ss"),
            ActivePower = GenerateRandomValue(1000.0, 10000.0), // Aktif güç (W)
            ReactivePower = GenerateRandomValue(0.0, 2000.0), // Reaktif güç (VAR)
            PowerFactor = GenerateRandomValue(0.8, 1.0), // Güç faktörü
            Phases = phases,
            InsulationResistance = GenerateRandomValue(1.0, 10.0), // MΩ
            DeviceSerialNumber = component.SerialNumber ?? $"INV{_random.Next(100000, 999999)}",
            FirmwareVersion = $"v1.1"
        };
    }

    private MPPSData GenerateMPPSData(DeviceComponentInfo component, DateTime timestamp)
    {
        return new MPPSData
        {
            InputVoltage = GenerateRandomValue(100.0, 500.0), // DC giriş voltajı
            InputCurrent = GenerateRandomValue(5.0, 50.0), // DC giriş akımı
            OutputVoltage = GenerateRandomValue(12.0, 48.0), // DC çıkış voltajı
            OutputCurrent = GenerateRandomValue(10.0, 100.0), // DC çıkış akımı
            Efficiency = GenerateRandomValue(85.0, 98.0), // Verimlilik %
            Temperature = GenerateRandomValue(25.0, 60.0), // Sıcaklık
            PowerOutput = GenerateRandomValue(100.0, 5000.0), // Güç çıkışı (W)
            DeviceSerialNumber = component.SerialNumber ?? $"MPPS{_random.Next(100000, 999999)}",
            FirmwareVersion = $"v1.1",
            DateTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private GenericData GenerateGenericData(DeviceType deviceType, DateTime timestamp)
    {
        return new GenericData
        {
            Value = GenerateRandomValue(0.0, 100.0),
            Unit = "unit",
            Timestamp = timestamp,
            DeviceType = deviceType.ToString()
        };
    }

    public T GenerateRandomValue<T>(T min, T max) where T : IComparable<T>
    {
        if (typeof(T) == typeof(double))
        {
            var minDouble = Convert.ToDouble(min);
            var maxDouble = Convert.ToDouble(max);
            var value = minDouble + (_random.NextDouble() * (maxDouble - minDouble));
            return (T)Convert.ChangeType(value, typeof(T));
        }
        
        if (typeof(T) == typeof(int))
        {
            var minInt = Convert.ToInt32(min);
            var maxInt = Convert.ToInt32(max);
            var value = _random.Next(minInt, maxInt + 1);
            return (T)Convert.ChangeType(value, typeof(T));
        }
        
        if (typeof(T) == typeof(float))
        {
            var minFloat = Convert.ToSingle(min);
            var maxFloat = Convert.ToSingle(max);
            var value = minFloat + ((float)_random.NextDouble() * (maxFloat - minFloat));
            return (T)Convert.ChangeType(value, typeof(T));
        }

        throw new NotSupportedException($"Type {typeof(T)} is not supported for random generation");
    }

}

// HEMS Data Models
public class BatteryData
{
    public double BatteryPackVoltage { get; set; }
    public double BatteryCurrent { get; set; }
    public double SOH { get; set; }
    public List<double> SOCCumulative { get; set; } = new();
    public List<BatteryCell> SOC { get; set; } = new();
    public string TotalChargeCapacity { get; set; } = string.Empty;
    public double TotalDischargeAmount { get; set; }
    public double MaxCellVoltage { get; set; }
    public double MinCellVoltage { get; set; }
    public double MaxCellTemperature { get; set; }
    public double MinCellTemperature { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string DateTime { get; set; } = string.Empty;
}

public class BatteryCell
{
    public int CellNumber { get; set; }
    public double CellVoltage { get; set; }
    public double CellTemperature { get; set; }
}

public class BatteryClusterData
{
    public double VoltagePack { get; set; }
    public double CurrentPack { get; set; }
    public double MaxVoltageOfBatteryCluster { get; set; }
    public double MinVoltageOfBatteryCluster { get; set; }
    public double SOC { get; set; }
    public double TotalChargeCapacity { get; set; }
    public double TotalDischargeCapacity { get; set; }
    public string DateTime { get; set; } = string.Empty;
}

public class MPPSData
{
    public double InputVoltage { get; set; }
    public double InputCurrent { get; set; }
    public double OutputVoltage { get; set; }
    public double OutputCurrent { get; set; }
    public double Efficiency { get; set; }
    public double Temperature { get; set; }
    public double PowerOutput { get; set; }
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string DateTime { get; set; } = string.Empty;
}

public class GenericData
{
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string DeviceType { get; set; } = string.Empty;
}


