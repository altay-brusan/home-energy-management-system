using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for proactively collecting daily energy data from devices via RPC.
/// Runs on a schedule and pulls data from devices at 23:45 local time.
/// Falls back to power data estimation if device is unavailable.
/// </summary>
public class EnergyDataCollectionService : IEnergyDataCollectionService
{
    private readonly IApplicationDbContext _context;
    private readonly IThingsBoardConnectorService _thingsBoardConnector;
    private readonly ITimezoneService _timezoneService;
    private readonly ILogger<EnergyDataCollectionService> _logger;
    private readonly IMemoryCache _collectionStatusCache;

    // Configuration constants
    private const int CollectionStartHour = 23;
    private const int CollectionStartMinute = 45;
    private const int CollectionDeadlineHour = 0;
    private const int RpcTimeoutMs = 10000;

    public EnergyDataCollectionService(
        IApplicationDbContext context,
        IThingsBoardConnectorService thingsBoardConnector,
        ITimezoneService timezoneService,
        ILogger<EnergyDataCollectionService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _thingsBoardConnector = thingsBoardConnector;
        _timezoneService = timezoneService;
        _logger = logger;
        _collectionStatusCache = cache;
    }

    public async Task ProcessEnergyDataCollectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting energy data collection cycle");

        // Get all active devices with SetupDevice (excludes demo devices)
        var devices = await _context.Devices
            .Where(d => !d.IsDeleted &&
                        !d.IsDemoDevice &&
                        d.SetupDevice != null)
            .Select(d => new { d.Id, d.ThingsBoardDeviceId })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {DeviceCount} devices to check for energy collection", devices.Count);

        foreach (var device in devices)
        {
            try
            {
                await ProcessDeviceIfNeededAsync(device.Id, device.ThingsBoardDeviceId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device {DeviceId} for energy collection", device.Id);
            }
        }

        _logger.LogInformation("Completed energy data collection cycle");
    }

    private async Task ProcessDeviceIfNeededAsync(
        Guid localDeviceId,
        Guid thingsBoardDeviceId,
        CancellationToken cancellationToken)
    {
        // Get device's local time
        var timezone = await _timezoneService.GetDeviceTimezoneAsync(localDeviceId, cancellationToken);
        var deviceLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var today = DateOnly.FromDateTime(deviceLocalTime);

        // Determine if we should process this device
        var shouldProcess = ShouldProcessDevice(deviceLocalTime, localDeviceId, today);
        if (!shouldProcess) return;

        _logger.LogInformation(
            "Device {DeviceId} is in collection window (local time: {LocalTime}), processing...",
            localDeviceId, deviceLocalTime);

        await ProcessDeviceEnergyDataAsync(localDeviceId, cancellationToken);
    }

    private bool ShouldProcessDevice(DateTime deviceLocalTime, Guid deviceId, DateOnly date)
    {
        var hour = deviceLocalTime.Hour;
        var minute = deviceLocalTime.Minute;

        // Check if within collection window (23:45 - 00:15)
        // Extended window to 00:15 to handle edge cases
        var inCollectionWindow = (hour == 23 && minute >= 45) || (hour == 0 && minute <= 15);
        if (!inCollectionWindow) return false;

        // Check if already successfully processed today
        var statusKey = GetStatusCacheKey(deviceId, date);
        if (_collectionStatusCache.TryGetValue<EnergyCollectionStatus>(statusKey, out var status))
        {
            if (status?.DailyRpcSucceeded == true && status.LifetimeRpcSucceeded == true)
            {
                _logger.LogDebug(
                    "Device {DeviceId} already processed for {Date}, skipping",
                    deviceId, date);
                return false;
            }
        }

        return true;
    }

    public async Task ProcessDeviceEnergyDataAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Devices
            .Where(d => d.Id == deviceId && !d.IsDeleted)
            .Select(d => new { d.ThingsBoardDeviceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (device == null)
        {
            _logger.LogWarning("Device {DeviceId} not found", deviceId);
            return;
        }

        var timezone = await _timezoneService.GetDeviceTimezoneAsync(deviceId, cancellationToken);
        var deviceLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        var today = DateOnly.FromDateTime(deviceLocalTime);

        // Determine the target date for energy data
        // If it's past midnight (0:00-0:15), we want yesterday's data
        var targetDate = deviceLocalTime.Hour == 0
            ? deviceLocalTime.Date.AddDays(-1)
            : deviceLocalTime.Date;

        _logger.LogInformation(
            "Processing energy data for device {DeviceId}, local time: {LocalTime}, target date: {TargetDate}",
            deviceId, deviceLocalTime, targetDate);

        var dailyRpcSuccess = false;
        var lifetimeRpcSuccess = false;
        string createdBy = EnergyDataSources.Device;

        // Step 1: Try RPC for daily energy
        var dailyEnergy = await FetchDailyEnergyViaRpcAsync(device.ThingsBoardDeviceId, cancellationToken);

        if (dailyEnergy != null)
        {
            dailyRpcSuccess = true;
            _logger.LogInformation(
                "Successfully fetched daily energy via RPC for device {DeviceId}",
                deviceId);
        }
        else if (deviceLocalTime.Hour == 0)
        {
            // RPC failed and we're at deadline - use estimation
            _logger.LogWarning(
                "Daily RPC failed for device {DeviceId} at deadline, using power data estimation",
                deviceId);

            var estimated = await EstimateDailyEnergyFromPowerDataAsync(
                device.ThingsBoardDeviceId,
                targetDate,
                cancellationToken);

            dailyEnergy = new DailyEnergyRpcResult
            {
                PvTotalEnergyKWh = estimated.PvTotalEnergyKWh,
                GridImportEnergyKWh = estimated.GridImportEnergyKWh,
                GridExportEnergyKWh = estimated.GridExportEnergyKWh,
                BatteryChargeEnergyKWh = estimated.BatteryChargeEnergyKWh,
                BatteryDischargeEnergyKWh = estimated.BatteryDischargeEnergyKWh,
                LoadEnergyKWh = estimated.LoadEnergyKWh,
                GeneratorEnergyKWh = estimated.GeneratorEnergyKWh
            };
            createdBy = EnergyDataSources.SystemEstimate;

            _logger.LogInformation(
                "Estimated daily energy from {PointCount} power data points for device {DeviceId}",
                estimated.PowerDataPointsUsed, deviceId);
        }
        else
        {
            // RPC failed but not at deadline - will retry on next cycle
            _logger.LogDebug(
                "Daily RPC failed for device {DeviceId}, will retry on next cycle",
                deviceId);
        }

        // Save daily energy if we have it
        if (dailyEnergy != null)
        {
            await SaveDailyEnergyDataAsync(
                device.ThingsBoardDeviceId,
                targetDate,
                dailyEnergy,
                createdBy,
                cancellationToken);
        }

        // Step 2: Try RPC for lifetime energy
        var lifetimeEnergy = await FetchLifetimeEnergyViaRpcAsync(device.ThingsBoardDeviceId, cancellationToken);

        if (lifetimeEnergy != null)
        {
            lifetimeRpcSuccess = true;
            _logger.LogInformation(
                "Successfully fetched lifetime energy via RPC for device {DeviceId}",
                deviceId);
        }
        else if (deviceLocalTime.Hour == 0 && dailyEnergy != null)
        {
            // RPC failed at deadline - calculate from previous lifetime + daily
            _logger.LogWarning(
                "Lifetime RPC failed for device {DeviceId} at deadline, calculating from previous + daily",
                deviceId);

            var dailyEstimate = new EstimatedDailyEnergy
            {
                PvTotalEnergyKWh = dailyEnergy.PvTotalEnergyKWh ?? 0,
                GridImportEnergyKWh = dailyEnergy.GridImportEnergyKWh ?? 0,
                GridExportEnergyKWh = dailyEnergy.GridExportEnergyKWh ?? 0,
                BatteryChargeEnergyKWh = dailyEnergy.BatteryChargeEnergyKWh ?? 0,
                BatteryDischargeEnergyKWh = dailyEnergy.BatteryDischargeEnergyKWh ?? 0,
                LoadEnergyKWh = dailyEnergy.LoadEnergyKWh ?? 0,
                GeneratorEnergyKWh = dailyEnergy.GeneratorEnergyKWh ?? 0,
                IsEstimated = createdBy == EnergyDataSources.SystemEstimate
            };

            var calculatedLifetime = await CalculateLifetimeEnergyAsync(
                device.ThingsBoardDeviceId,
                dailyEstimate,
                cancellationToken);

            lifetimeEnergy = new LifetimeEnergyRpcResult
            {
                PvTotalEnergyKWh = calculatedLifetime.PvTotalEnergyKWh,
                GridImportEnergyKWh = calculatedLifetime.GridImportEnergyKWh,
                GridExportEnergyKWh = calculatedLifetime.GridExportEnergyKWh,
                BatteryChargeEnergyKWh = calculatedLifetime.BatteryChargeEnergyKWh,
                BatteryDischargeEnergyKWh = calculatedLifetime.BatteryDischargeEnergyKWh,
                LoadEnergyKWh = calculatedLifetime.LoadEnergyKWh,
                GeneratorEnergyKWh = calculatedLifetime.GeneratorEnergyKWh
            };
        }

        // Save lifetime energy if we have it
        if (lifetimeEnergy != null)
        {
            await SaveLifetimeEnergyDataAsync(
                device.ThingsBoardDeviceId,
                targetDate,
                lifetimeEnergy,
                createdBy,
                cancellationToken);
        }

        // Update status cache
        UpdateCollectionStatus(deviceId, today, dailyRpcSuccess || dailyEnergy != null, lifetimeRpcSuccess || lifetimeEnergy != null);
    }

    public async Task<DailyEnergyRpcResult?> FetchDailyEnergyViaRpcAsync(
        Guid thingsBoardDeviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling getDailyEnergyFlowState RPC for device {DeviceId}", thingsBoardDeviceId);

            var response = await _thingsBoardConnector.SendTwoWayRpcAsync(
                thingsBoardDeviceId,
                "getDailyEnergyFlowState",
                new Dictionary<string, object>(),
                RpcTimeoutMs,
                10,
                cancellationToken);

            if (response?.Success != true || response.Response == null)
            {
                _logger.LogWarning(
                    "getDailyEnergyFlowState RPC failed for device {DeviceId}: {Error}",
                    thingsBoardDeviceId, response?.Error ?? "No response");
                return null;
            }

            return ParseDailyEnergyResponse(response.Response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily energy via RPC for device {DeviceId}", thingsBoardDeviceId);
            return null;
        }
    }

    public async Task<LifetimeEnergyRpcResult?> FetchLifetimeEnergyViaRpcAsync(
        Guid thingsBoardDeviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling getTotalEnergyFlowState RPC for device {DeviceId}", thingsBoardDeviceId);

            var response = await _thingsBoardConnector.SendTwoWayRpcAsync(
                thingsBoardDeviceId,
                "getTotalEnergyFlowState",
                new Dictionary<string, object>(),
                RpcTimeoutMs,
                10,
                cancellationToken);

            if (response?.Success != true || response.Response == null)
            {
                _logger.LogWarning(
                    "getTotalEnergyFlowState RPC failed for device {DeviceId}: {Error}",
                    thingsBoardDeviceId, response?.Error ?? "No response");
                return null;
            }

            return ParseLifetimeEnergyResponse(response.Response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lifetime energy via RPC for device {DeviceId}", thingsBoardDeviceId);
            return null;
        }
    }

    public async Task<EstimatedDailyEnergy> EstimateDailyEnergyFromPowerDataAsync(
        Guid thingsBoardDeviceId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        // Get all power readings for the date
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var powerReadings = await _context.DevicePowerData
            .Where(p => p.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       p.DeviceLocalTime >= startOfDay &&
                       p.DeviceLocalTime < endOfDay)
            .OrderBy(p => p.DeviceLocalTime)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Estimating daily energy for device {DeviceId} from {Count} power readings on {Date}",
            thingsBoardDeviceId, powerReadings.Count, date.ToString("yyyy-MM-dd"));

        if (powerReadings.Count < 2)
        {
            _logger.LogWarning(
                "Insufficient power data for device {DeviceId} on {Date} (only {Count} readings)",
                thingsBoardDeviceId, date.ToString("yyyy-MM-dd"), powerReadings.Count);

            return new EstimatedDailyEnergy
            {
                PowerDataPointsUsed = powerReadings.Count,
                IsEstimated = true
            };
        }

        // Calculate energy using trapezoidal integration
        double pvEnergy = 0, gridImport = 0, gridExport = 0;
        double batteryCharge = 0, batteryDischarge = 0;
        double loadEnergy = 0, generatorEnergy = 0;

        for (int i = 1; i < powerReadings.Count; i++)
        {
            var prev = powerReadings[i - 1];
            var curr = powerReadings[i];
            var intervalHours = (curr.DeviceLocalTime - prev.DeviceLocalTime).TotalHours;

            // Skip if interval is too large (> 30 min) - indicates data gap
            if (intervalHours > 0.5)
            {
                _logger.LogDebug(
                    "Skipping large interval ({Interval:F2}h) between {Time1} and {Time2}",
                    intervalHours, prev.DeviceLocalTime, curr.DeviceLocalTime);
                continue;
            }

            // PV: always positive
            pvEnergy += CalculateEnergy(prev.PvTotalPowerW, curr.PvTotalPowerW, intervalHours);

            // Grid: positive = import, negative = export
            var avgGridPower = ((prev.GridTotalPowerW ?? 0) + (curr.GridTotalPowerW ?? 0)) / 2;
            if (avgGridPower > 0)
                gridImport += avgGridPower * intervalHours / 1000;
            else
                gridExport += Math.Abs(avgGridPower) * intervalHours / 1000;

            // Battery: positive = discharge, negative = charge
            var avgBatteryPower = ((prev.BatteryPowerW ?? 0) + (curr.BatteryPowerW ?? 0)) / 2;
            if (avgBatteryPower > 0)
                batteryDischarge += avgBatteryPower * intervalHours / 1000;
            else
                batteryCharge += Math.Abs(avgBatteryPower) * intervalHours / 1000;

            // Load: always positive
            loadEnergy += CalculateEnergy(prev.LoadTotalPowerW, curr.LoadTotalPowerW, intervalHours);

            // Generator: always positive
            generatorEnergy += CalculateEnergy(prev.GeneratorTotalPowerW, curr.GeneratorTotalPowerW, intervalHours);
        }

        return new EstimatedDailyEnergy
        {
            PvTotalEnergyKWh = Math.Round(pvEnergy, 2),
            GridImportEnergyKWh = Math.Round(gridImport, 2),
            GridExportEnergyKWh = Math.Round(gridExport, 2),
            BatteryChargeEnergyKWh = Math.Round(batteryCharge, 2),
            BatteryDischargeEnergyKWh = Math.Round(batteryDischarge, 2),
            LoadEnergyKWh = Math.Round(loadEnergy, 2),
            GeneratorEnergyKWh = Math.Round(generatorEnergy, 2),
            IsEstimated = true,
            PowerDataPointsUsed = powerReadings.Count
        };
    }

    public async Task<CalculatedLifetimeEnergy> CalculateLifetimeEnergyAsync(
        Guid thingsBoardDeviceId,
        EstimatedDailyEnergy dailyEnergy,
        CancellationToken cancellationToken = default)
    {
        // Get the most recent lifetime energy record
        var lastLifetime = await _context.DeviceEnergyData
            .Where(e => e.ThingsBoardDeviceId == thingsBoardDeviceId &&
                       e.DataType == DataTypes.LifetimeEnergy)
            .OrderByDescending(e => e.DeviceLocalTime)
            .FirstOrDefaultAsync(cancellationToken);

        _logger.LogDebug(
            "Calculating lifetime energy for device {DeviceId}, previous lifetime exists: {HasPrevious}",
            thingsBoardDeviceId, lastLifetime != null);

        // Add daily energy to previous lifetime
        return new CalculatedLifetimeEnergy
        {
            PvTotalEnergyKWh = Math.Round((lastLifetime?.PvTotalEnergyKWh ?? 0) + dailyEnergy.PvTotalEnergyKWh, 2),
            GridImportEnergyKWh = Math.Round((lastLifetime?.GridImportEnergyKWh ?? 0) + dailyEnergy.GridImportEnergyKWh, 2),
            GridExportEnergyKWh = Math.Round((lastLifetime?.GridExportEnergyKWh ?? 0) + dailyEnergy.GridExportEnergyKWh, 2),
            BatteryChargeEnergyKWh = Math.Round((lastLifetime?.BatteryChargeEnergyKWh ?? 0) + dailyEnergy.BatteryChargeEnergyKWh, 2),
            BatteryDischargeEnergyKWh = Math.Round((lastLifetime?.BatteryDischargeEnergyKWh ?? 0) + dailyEnergy.BatteryDischargeEnergyKWh, 2),
            LoadEnergyKWh = Math.Round((lastLifetime?.LoadEnergyKWh ?? 0) + dailyEnergy.LoadEnergyKWh, 2),
            GeneratorEnergyKWh = Math.Round((lastLifetime?.GeneratorEnergyKWh ?? 0) + dailyEnergy.GeneratorEnergyKWh, 2)
        };
    }

    #region Helper Methods

    private static double CalculateEnergy(double? prevPower, double? currPower, double intervalHours)
    {
        // Trapezoidal rule: average of two power values * time
        var avgPower = ((prevPower ?? 0) + (currPower ?? 0)) / 2;
        return Math.Max(0, avgPower * intervalHours / 1000); // Convert W to kWh
    }

    /// <summary>
    /// Parses getDailyEnergyFlowState response with nested JSON structure:
    /// { loadEnergyKWh, grid: { importEnergyKWh, exportEnergyKWh }, pv: { totalEnergyKWh, mppt: [...] },
    ///   battery: { chargeEnergyKWh, dischargeEnergyKWh }, generator: { energyKWh } }
    /// </summary>
    private static DailyEnergyRpcResult ParseDailyEnergyResponse(Dictionary<string, object> response)
    {
        // Local variables for building the result
        double? loadEnergy = GetDoubleValue(response, "loadEnergyKWh");
        double? gridImport = null, gridExport = null;
        double? pvTotal = null;
        double? mppt1 = null, mppt2 = null, mppt3 = null, mppt4 = null;
        double? batteryCharge = null, batteryDischarge = null;
        double? generatorEnergy = null;

        // Grid (nested)
        var grid = GetNestedObject(response, "grid");
        if (grid != null)
        {
            gridImport = GetDoubleValue(grid, "importEnergyKWh");
            gridExport = GetDoubleValue(grid, "exportEnergyKWh");
        }

        // PV (nested)
        var pv = GetNestedObject(response, "pv");
        if (pv != null)
        {
            pvTotal = GetDoubleValue(pv, "totalEnergyKWh");

            // Parse MPPT array
            var mpptArray = GetNestedArray(pv, "mppt");
            if (mpptArray != null)
            {
                foreach (var mppt in mpptArray)
                {
                    var id = GetIntValue(mppt, "id");
                    var energy = GetDoubleValue(mppt, "energyKWh");
                    if (id.HasValue && energy.HasValue)
                    {
                        switch (id.Value)
                        {
                            case 1: mppt1 = energy; break;
                            case 2: mppt2 = energy; break;
                            case 3: mppt3 = energy; break;
                            case 4: mppt4 = energy; break;
                        }
                    }
                }
            }
        }

        // Battery (nested)
        var battery = GetNestedObject(response, "battery");
        if (battery != null)
        {
            batteryCharge = GetDoubleValue(battery, "chargeEnergyKWh");
            batteryDischarge = GetDoubleValue(battery, "dischargeEnergyKWh");
        }

        // Generator (nested)
        var generator = GetNestedObject(response, "generator");
        if (generator != null)
        {
            generatorEnergy = GetDoubleValue(generator, "energyKWh");
        }

        return new DailyEnergyRpcResult
        {
            LoadEnergyKWh = loadEnergy,
            GridImportEnergyKWh = gridImport,
            GridExportEnergyKWh = gridExport,
            PvTotalEnergyKWh = pvTotal,
            PvMppt1EnergyKWh = mppt1,
            PvMppt2EnergyKWh = mppt2,
            PvMppt3EnergyKWh = mppt3,
            PvMppt4EnergyKWh = mppt4,
            BatteryChargeEnergyKWh = batteryCharge,
            BatteryDischargeEnergyKWh = batteryDischarge,
            GeneratorEnergyKWh = generatorEnergy
        };
    }

    /// <summary>
    /// Parses getTotalEnergyFlowState response with nested JSON structure:
    /// { grid: { importTotalEnergyKWh, exportTotalEnergyKWh }, pv: { totalEnergyKWh, mppt: [...] },
    ///   load: { totalEnergyKWh }, battery: { totalChargeEnergyKWh, totalDischargeEnergyKWh },
    ///   generator: { totalEnergyKWh } }
    /// </summary>
    private static LifetimeEnergyRpcResult ParseLifetimeEnergyResponse(Dictionary<string, object> response)
    {
        // Local variables for building the result
        double? gridImport = null, gridExport = null;
        double? pvTotal = null;
        double? mppt1 = null, mppt2 = null, mppt3 = null, mppt4 = null;
        double? loadEnergy = null;
        double? batteryCharge = null, batteryDischarge = null;
        double? generatorEnergy = null;

        // Grid (nested)
        var grid = GetNestedObject(response, "grid");
        if (grid != null)
        {
            gridImport = GetDoubleValue(grid, "importTotalEnergyKWh");
            gridExport = GetDoubleValue(grid, "exportTotalEnergyKWh");
        }

        // PV (nested)
        var pv = GetNestedObject(response, "pv");
        if (pv != null)
        {
            pvTotal = GetDoubleValue(pv, "totalEnergyKWh");

            // Parse MPPT array
            var mpptArray = GetNestedArray(pv, "mppt");
            if (mpptArray != null)
            {
                foreach (var mppt in mpptArray)
                {
                    var id = GetIntValue(mppt, "id");
                    var energy = GetDoubleValue(mppt, "totalEnergyKWh");
                    if (id.HasValue && energy.HasValue)
                    {
                        switch (id.Value)
                        {
                            case 1: mppt1 = energy; break;
                            case 2: mppt2 = energy; break;
                            case 3: mppt3 = energy; break;
                            case 4: mppt4 = energy; break;
                        }
                    }
                }
            }
        }

        // Load (nested - different from daily which has it at root)
        var load = GetNestedObject(response, "load");
        if (load != null)
        {
            loadEnergy = GetDoubleValue(load, "totalEnergyKWh");
        }

        // Battery (nested)
        var battery = GetNestedObject(response, "battery");
        if (battery != null)
        {
            batteryCharge = GetDoubleValue(battery, "totalChargeEnergyKWh");
            batteryDischarge = GetDoubleValue(battery, "totalDischargeEnergyKWh");
        }

        // Generator (nested)
        var generator = GetNestedObject(response, "generator");
        if (generator != null)
        {
            generatorEnergy = GetDoubleValue(generator, "totalEnergyKWh");
        }

        return new LifetimeEnergyRpcResult
        {
            GridImportEnergyKWh = gridImport,
            GridExportEnergyKWh = gridExport,
            PvTotalEnergyKWh = pvTotal,
            PvMppt1EnergyKWh = mppt1,
            PvMppt2EnergyKWh = mppt2,
            PvMppt3EnergyKWh = mppt3,
            PvMppt4EnergyKWh = mppt4,
            LoadEnergyKWh = loadEnergy,
            BatteryChargeEnergyKWh = batteryCharge,
            BatteryDischargeEnergyKWh = batteryDischarge,
            GeneratorEnergyKWh = generatorEnergy
        };
    }

    private static double? GetDoubleValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number)
                    return element.GetDouble();
                if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var d))
                    return d;
                return null;
            }
            if (value is double d1)
                return d1;
            if (value is int i)
                return i;
            if (value is long l)
                return l;
            if (value is float f)
                return f;
            if (double.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static int? GetIntValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number)
                    return element.GetInt32();
                if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var i))
                    return i;
                return null;
            }
            if (value is int i1)
                return i1;
            if (value is long l)
                return (int)l;
            if (int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    /// <summary>
    /// Gets a nested object from a dictionary, handling JsonElement conversion
    /// </summary>
    private static Dictionary<string, object>? GetNestedObject(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var result = new Dictionary<string, object>();
                foreach (var prop in element.EnumerateObject())
                {
                    result[prop.Name] = prop.Value;
                }
                return result;
            }
            return null;
        }

        if (value is Dictionary<string, object> dictValue)
            return dictValue;

        return null;
    }

    /// <summary>
    /// Gets a nested array from a dictionary, handling JsonElement conversion
    /// </summary>
    private static List<Dictionary<string, object>>? GetNestedArray(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var result = new List<Dictionary<string, object>>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var itemDict = new Dictionary<string, object>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            itemDict[prop.Name] = prop.Value;
                        }
                        result.Add(itemDict);
                    }
                }
                return result;
            }
            return null;
        }

        if (value is List<Dictionary<string, object>> listValue)
            return listValue;

        return null;
    }

    private async Task SaveDailyEnergyDataAsync(
        Guid thingsBoardDeviceId,
        DateTime date,
        DailyEnergyRpcResult data,
        string createdBy,
        CancellationToken cancellationToken)
    {
        // Check for existing record on this date
        var existing = await _context.DeviceEnergyData
            .FirstOrDefaultAsync(e =>
                e.ThingsBoardDeviceId == thingsBoardDeviceId &&
                e.DeviceLocalTime.Date == date.Date &&
                e.DataType == DataTypes.DailyCumulativeEnergy,
                cancellationToken);

        if (existing != null)
        {
            existing.PvTotalEnergyKWh = data.PvTotalEnergyKWh;
            existing.GridImportEnergyKWh = data.GridImportEnergyKWh;
            existing.GridExportEnergyKWh = data.GridExportEnergyKWh;
            existing.BatteryChargeEnergyKWh = data.BatteryChargeEnergyKWh;
            existing.BatteryDischargeEnergyKWh = data.BatteryDischargeEnergyKWh;
            existing.LoadEnergyKWh = data.LoadEnergyKWh;
            existing.GeneratorEnergyKWh = data.GeneratorEnergyKWh;
            existing.PvMppt1EnergyKWh = data.PvMppt1EnergyKWh;
            existing.PvMppt2EnergyKWh = data.PvMppt2EnergyKWh;
            existing.PvMppt3EnergyKWh = data.PvMppt3EnergyKWh;
            existing.PvMppt4EnergyKWh = data.PvMppt4EnergyKWh;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = createdBy;

            _logger.LogInformation(
                "Updated existing daily energy record for device {DeviceId} on {Date}",
                thingsBoardDeviceId, date.ToString("yyyy-MM-dd"));
        }
        else
        {
            var entity = new DeviceEnergyData
            {
                ThingsBoardDeviceId = thingsBoardDeviceId,
                DeviceLocalTime = date.Date.AddHours(23).AddMinutes(59), // End of day
                DataType = DataTypes.DailyCumulativeEnergy,
                MessageType = MessageTypes.Telemetry,
                PhaseType = PhaseTypes.SinglePhase,
                PvTotalEnergyKWh = data.PvTotalEnergyKWh,
                GridImportEnergyKWh = data.GridImportEnergyKWh,
                GridExportEnergyKWh = data.GridExportEnergyKWh,
                BatteryChargeEnergyKWh = data.BatteryChargeEnergyKWh,
                BatteryDischargeEnergyKWh = data.BatteryDischargeEnergyKWh,
                LoadEnergyKWh = data.LoadEnergyKWh,
                GeneratorEnergyKWh = data.GeneratorEnergyKWh,
                PvMppt1EnergyKWh = data.PvMppt1EnergyKWh,
                PvMppt2EnergyKWh = data.PvMppt2EnergyKWh,
                PvMppt3EnergyKWh = data.PvMppt3EnergyKWh,
                PvMppt4EnergyKWh = data.PvMppt4EnergyKWh,
                Created = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.DeviceEnergyData.Add(entity);

            _logger.LogInformation(
                "Created new daily energy record for device {DeviceId} on {Date} (source: {Source})",
                thingsBoardDeviceId, date.ToString("yyyy-MM-dd"), createdBy);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveLifetimeEnergyDataAsync(
        Guid thingsBoardDeviceId,
        DateTime date,
        LifetimeEnergyRpcResult data,
        string createdBy,
        CancellationToken cancellationToken)
    {
        // Check for existing lifetime record on this date
        var existing = await _context.DeviceEnergyData
            .FirstOrDefaultAsync(e =>
                e.ThingsBoardDeviceId == thingsBoardDeviceId &&
                e.DeviceLocalTime.Date == date.Date &&
                e.DataType == DataTypes.LifetimeEnergy,
                cancellationToken);

        if (existing != null)
        {
            existing.PvTotalEnergyKWh = data.PvTotalEnergyKWh;
            existing.GridImportEnergyKWh = data.GridImportEnergyKWh;
            existing.GridExportEnergyKWh = data.GridExportEnergyKWh;
            existing.BatteryChargeEnergyKWh = data.BatteryChargeEnergyKWh;
            existing.BatteryDischargeEnergyKWh = data.BatteryDischargeEnergyKWh;
            existing.LoadEnergyKWh = data.LoadEnergyKWh;
            existing.GeneratorEnergyKWh = data.GeneratorEnergyKWh;
            existing.PvMppt1EnergyKWh = data.PvMppt1EnergyKWh;
            existing.PvMppt2EnergyKWh = data.PvMppt2EnergyKWh;
            existing.PvMppt3EnergyKWh = data.PvMppt3EnergyKWh;
            existing.PvMppt4EnergyKWh = data.PvMppt4EnergyKWh;
            existing.LastModified = DateTime.UtcNow;
            existing.LastModifiedBy = createdBy;

            _logger.LogInformation(
                "Updated existing lifetime energy record for device {DeviceId} on {Date}",
                thingsBoardDeviceId, date.ToString("yyyy-MM-dd"));
        }
        else
        {
            var entity = new DeviceEnergyData
            {
                ThingsBoardDeviceId = thingsBoardDeviceId,
                DeviceLocalTime = date.Date.AddHours(23).AddMinutes(59), // End of day
                DataType = DataTypes.LifetimeEnergy,
                MessageType = MessageTypes.Telemetry,
                PhaseType = PhaseTypes.SinglePhase,
                PvTotalEnergyKWh = data.PvTotalEnergyKWh,
                GridImportEnergyKWh = data.GridImportEnergyKWh,
                GridExportEnergyKWh = data.GridExportEnergyKWh,
                BatteryChargeEnergyKWh = data.BatteryChargeEnergyKWh,
                BatteryDischargeEnergyKWh = data.BatteryDischargeEnergyKWh,
                LoadEnergyKWh = data.LoadEnergyKWh,
                GeneratorEnergyKWh = data.GeneratorEnergyKWh,
                PvMppt1EnergyKWh = data.PvMppt1EnergyKWh,
                PvMppt2EnergyKWh = data.PvMppt2EnergyKWh,
                PvMppt3EnergyKWh = data.PvMppt3EnergyKWh,
                PvMppt4EnergyKWh = data.PvMppt4EnergyKWh,
                Created = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.DeviceEnergyData.Add(entity);

            _logger.LogInformation(
                "Created new lifetime energy record for device {DeviceId} on {Date} (source: {Source})",
                thingsBoardDeviceId, date.ToString("yyyy-MM-dd"), createdBy);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string GetStatusCacheKey(Guid deviceId, DateOnly date)
    {
        return $"EnergyCollection_{deviceId}_{date:yyyy-MM-dd}";
    }

    private void UpdateCollectionStatus(Guid deviceId, DateOnly date, bool dailySuccess, bool lifetimeSuccess)
    {
        var statusKey = GetStatusCacheKey(deviceId, date);
        var status = new EnergyCollectionStatus
        {
            DeviceId = deviceId,
            Date = date.ToDateTime(TimeOnly.MinValue),
            DailyRpcSucceeded = dailySuccess,
            LifetimeRpcSucceeded = lifetimeSuccess,
            LastAttemptUtc = DateTime.UtcNow
        };

        // Cache for 2 hours - enough to cover the collection window
        _collectionStatusCache.Set(statusKey, status, TimeSpan.FromHours(2));
    }

    #endregion
}
