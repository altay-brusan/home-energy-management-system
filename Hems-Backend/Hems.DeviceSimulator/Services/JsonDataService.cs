using System.Text.Json;
using Hems.DeviceSimulator.Models;

namespace Hems.DeviceSimulator.Services;

public interface IJsonDataService
{
    Task<List<EnergyFlowData>> GetLifetimeEnergyDataAsync();
    Task<List<EnergyFlowData>> GetTodayEnergyDataAsync(int intervalMinutes = 10);
    Task<EnergyFlowData?> GetEnergyDataByTimeAsync(DateTime targetTime, bool isLifetime = false);
    Task<List<InverterData>> GetInverterDataByDateAsync(DateTime targetDate);
    Task<InverterData?> GetInverterDataByTimeAsync(DateTime targetTime);
    Task<List<Models.BatteryClusterData>> GetBMSDataByDateAsync(DateTime targetDate);
    Task<Models.BatteryClusterData?> GetBMSDataByTimeAsync(DateTime targetTime);
    Task<List<BatteryDataJson>> GetBatteryDataByDateAsync(DateTime targetDate);
    Task<BatteryDataJson?> GetBatteryDataByTimeAsync(DateTime targetTime);
}

public class JsonDataService : IJsonDataService
{
    private readonly ILogger<JsonDataService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _dataPath;
    
    private List<EnergyFlowData>? _lifetimeData;
    private List<EnergyFlowData>? _todayData10Min;
    private List<EnergyFlowData>? _todayData2Min;

    public JsonDataService(ILogger<JsonDataService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataJsonFiles");
    }

    public async Task<List<EnergyFlowData>> GetLifetimeEnergyDataAsync()
    {
        if (_lifetimeData != null)
            return _lifetimeData;

        try
        {
            var filePath = Path.Combine(_dataPath, "lifetime_energy_flow.json");
            var jsonContent = await File.ReadAllTextAsync(filePath);
            _lifetimeData = JsonSerializer.Deserialize<List<EnergyFlowData>>(jsonContent) ?? new List<EnergyFlowData>();
            
            _logger.LogInformation("📊 Loaded {Count} lifetime energy data records", _lifetimeData.Count);
            return _lifetimeData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading lifetime energy data");
            return new List<EnergyFlowData>();
        }
    }

    public async Task<List<EnergyFlowData>> GetTodayEnergyDataAsync(int intervalMinutes = 10)
    {
        var data = intervalMinutes switch
        {
            2 => _todayData2Min,
            10 => _todayData10Min,
            _ => _todayData10Min
        };

        if (data != null)
            return data;

        try
        {
            var fileName = intervalMinutes == 2 ? "today_energy_flow_2min.json" : "today_energy_flow_10min.json";
            var filePath = Path.Combine(_dataPath, fileName);
            var jsonContent = await File.ReadAllTextAsync(filePath);
            data = JsonSerializer.Deserialize<List<EnergyFlowData>>(jsonContent) ?? new List<EnergyFlowData>();
            
            if (intervalMinutes == 2)
                _todayData2Min = data;
            else
                _todayData10Min = data;
            
            _logger.LogInformation("📊 Loaded {Count} today energy data records ({Interval}min interval)", 
                data.Count, intervalMinutes);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading today energy data");
            return new List<EnergyFlowData>();
        }
    }

    public async Task<EnergyFlowData?> GetEnergyDataByTimeAsync(DateTime targetTime, bool isLifetime = false)
    {
        try
        {
            var data = isLifetime 
                ? await GetLifetimeEnergyDataAsync()
                : await GetTodayEnergyDataAsync();

            if (!data.Any())
                return null;

            // Lifetime data için: gece yarısı (00:00:00) tarihine göre bul
            if (isLifetime)
            {
                var targetDate = targetTime.Date;
                return data.FirstOrDefault(d => d.DateTime.Date == targetDate);
            }

            // Today data için: saatine göre bul (saat başı çalışıyor)
            var targetHour = targetTime.Hour;
            return data.FirstOrDefault(d => d.DateTime.Hour == targetHour);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting energy data by time");
            return null;
        }
    }






    public async Task<List<InverterData>> GetInverterDataByDateAsync(DateTime targetDate)
    {
        try
        {
            // Bugünün gününe göre dosya seç (Pazartesi=1, Pazar=0)
            var dayOfWeek = targetDate.DayOfWeek;
            var dayIndex = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1; // Pazar=6, Pazartesi=0
            var dayNumber = 3 + dayIndex; // 3'ten başlayarak 3,4,5,6,7,8,9
            
            var fileName = $"inverter_output_1min_2025-10-0{dayNumber}.json";
            var filePath = Path.Combine(_dataPath, "Inverter", fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Inverter data file not found: {FilePath}", filePath);
                return new List<InverterData>();
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var inverterData = JsonSerializer.Deserialize<List<InverterData>>(jsonContent) ?? new List<InverterData>();
            
            _logger.LogInformation("📊 Loaded {Count} inverter data records for {Date} (Day {DayNumber})", 
                inverterData.Count, targetDate.ToString("yyyy-MM-dd"), dayNumber);
            
            return inverterData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inverter data for date {Date}", targetDate);
            return new List<InverterData>();
        }
    }

    public async Task<InverterData?> GetInverterDataByTimeAsync(DateTime targetTime)
    {
        try
        {
            // Günlük inverter verisini al
            var inverterData = await GetInverterDataByDateAsync(targetTime.Date);
            
            if (!inverterData.Any())
                return null;

            // Saat ve dakikaya göre en yakın kaydı bul
            var targetTimeOfDay = targetTime.TimeOfDay;
            return inverterData.OrderBy(d => 
            {
                if (DateTime.TryParse(d.DateTime, out var dataTime))
                {
                    return Math.Abs((dataTime.TimeOfDay - targetTimeOfDay).Ticks);
                }
                return long.MaxValue;
            }).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inverter data by time");
            return null;
        }
    }

    public async Task<List<Models.BatteryClusterData>> GetBMSDataByDateAsync(DateTime targetDate)
    {
        try
        {
            // Bugünün gününe göre dosya seç (Pazartesi=1, Pazar=0)
            var dayOfWeek = targetDate.DayOfWeek;
            var dayIndex = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1; // Pazar=6, Pazartesi=0
            var dayNumber = 3 + dayIndex; // 3'ten başlayarak 3,4,5,6,7,8,9
            
            var fileName = $"batteri_cluster_anlik_veri_seti_1min_2025-10-0{dayNumber}.json";
            var filePath = Path.Combine(_dataPath, "BMS", fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("BMS data file not found: {FilePath}", filePath);
                return new List<Models.BatteryClusterData>();
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var bmsData = JsonSerializer.Deserialize<List<Models.BatteryClusterData>>(jsonContent) ?? new List<Models.BatteryClusterData>();
            
            _logger.LogInformation("📊 Loaded {Count} BMS data records for {Date} (Day {DayNumber})", 
                bmsData.Count, targetDate.ToString("yyyy-MM-dd"), dayNumber);
            
            return bmsData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading BMS data for date {Date}", targetDate);
            return new List<Models.BatteryClusterData>();
        }
    }

    public async Task<Models.BatteryClusterData?> GetBMSDataByTimeAsync(DateTime targetTime)
    {
        try
        {
            // Günlük BMS verisini al
            var bmsData = await GetBMSDataByDateAsync(targetTime.Date);
            
            if (!bmsData.Any())
                return null;

            // Saat ve dakikaya göre en yakın kaydı bul
            var targetTimeOfDay = targetTime.TimeOfDay;
            return bmsData.OrderBy(d => 
            {
                if (DateTime.TryParse(d.DateTime, out var dataTime))
                {
                    return Math.Abs((dataTime.TimeOfDay - targetTimeOfDay).Ticks);
                }
                return long.MaxValue;
            }).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BMS data by time");
            return null;
        }
    }

    public async Task<List<BatteryDataJson>> GetBatteryDataByDateAsync(DateTime targetDate)
    {
        try
        {
            // Bugünün gününe göre dosya seç (Pazartesi=1, Pazar=0)
            var dayOfWeek = targetDate.DayOfWeek;
            var dayIndex = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1; // Pazar=6, Pazartesi=0
            var dayNumber = 3 + dayIndex; // 3'ten başlayarak 3,4,5,6,7,8,9
            
            var fileName = $"Battery_dual_1min_2025-10-0{dayNumber}.json";
            var filePath = Path.Combine(_dataPath, "Battery", fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Battery data file not found: {FilePath}", filePath);
                return new List<BatteryDataJson>();
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var batteryData = JsonSerializer.Deserialize<List<BatteryDataJson>>(jsonContent) ?? new List<BatteryDataJson>();
            
            _logger.LogInformation("📊 Loaded {Count} battery data records for {Date} (Day {DayNumber})", 
                batteryData.Count, targetDate.ToString("yyyy-MM-dd"), dayNumber);
            
            return batteryData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading battery data for date {Date}", targetDate);
            return new List<BatteryDataJson>();
        }
    }

    public async Task<BatteryDataJson?> GetBatteryDataByTimeAsync(DateTime targetTime)
    {
        try
        {
            // Günlük battery verisini al
            var batteryData = await GetBatteryDataByDateAsync(targetTime.Date);
            
            if (!batteryData.Any())
                return null;

            // Saat ve dakikaya göre en yakın kaydı bul
            var targetTimeOfDay = targetTime.TimeOfDay;
            return batteryData.OrderBy(d => 
            {
                if (DateTime.TryParse(d.DateTime, out var dataTime))
                {
                    return Math.Abs((dataTime.TimeOfDay - targetTimeOfDay).Ticks);
                }
                return long.MaxValue;
            }).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting battery data by time");
            return null;
        }
    }
}
