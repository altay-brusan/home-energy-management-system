using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Dashboard.Queries.GetSoc;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Microsoft.Extensions.Caching.Memory;

namespace Hems_Backend.Application.Dashboard.Queries.GetTodayEnergySummary;

public class GetDailyEnergySummaryHandler : IRequestHandler<GetDailyEnergySummaryQuery, DailyEnergySummaryDto>
{
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IThingsBoardConnectorService _tbService;
    private readonly IMemoryCache _cache;
    private readonly IDemoDataService _demoDataService;
    private readonly IDeviceActivityService _activityService;

    public GetDailyEnergySummaryHandler(
        ISender sender,
        IDeviceMappingService deviceMapping,
        IThingsBoardConnectorService tbService,
        IMemoryCache cache,
        IDemoDataService demoDataService,
        IDeviceActivityService activityService)
    {
        _sender = sender;
        _deviceMapping = deviceMapping;
        _tbService = tbService;
        _cache = cache;
        _demoDataService = demoDataService;
        _activityService = activityService;
    }

    public async Task<DailyEnergySummaryDto> Handle(GetDailyEnergySummaryQuery request, CancellationToken cancellationToken)
    {
        // Device access check (uses local DeviceId)
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
        }

        // Check if demo device - return simulated data
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(request.DeviceId, cancellationToken);
        if (isDemoDevice)
        {
            return GetSimulatedDailyEnergySummary(request.DeviceId);
        }

        // Define cache key based on device ID
        var cacheKey = $"DailyEnergySummary_{request.DeviceId}";

        // Try to get cached data first
        if (_cache.TryGetValue<DailyEnergySummaryDto>(cacheKey, out var cachedData))
        {
            return cachedData!;
        }

        // Map local DeviceId to ThingsBoardDeviceId for RPC call
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(request.DeviceId, cancellationToken);
        if (thingsBoardDeviceId == null)
        {
            throw new ArgumentException($"Device with ID {request.DeviceId} not found or has no ThingsBoard mapping.");
        }

        // Call RPC method to get daily energy flow state from device
        var rpcResponse = await _tbService.SendTwoWayRpcAsync(
            deviceId: thingsBoardDeviceId.Value,
            method: "getDailyEnergyFlowState",
            parameters: new Dictionary<string, object>(),
            timeoutMs: 10000,
            cancellationToken: cancellationToken
        );

        // Check if RPC call failed
        if (rpcResponse == null || !rpcResponse.Success || rpcResponse.Response == null)
        {
            // Return empty response if RPC failed
            return new DailyEnergySummaryDto
            {
                DeviceLocalTime = DateTime.UtcNow,
                DeviceId = request.DeviceId,
                GridImport = 0.0,
                GridExport = 0.0,
                Consumption = 0.0,
                Production = 0.0
            };
        }

        // Parse the device response
        DailyEnergyFlowStateDto? deviceData;
        try
        {
            // Response may be wrapped: {"success": true, "data": {...}}
            // Extract the "data" property if present
            if (rpcResponse.Response.TryGetValue("data", out var dataObj) && dataObj != null)
            {
                var dataJson = JsonSerializer.Serialize(dataObj);
                deviceData = JsonSerializer.Deserialize<DailyEnergyFlowStateDto>(dataJson);
            }
            else
            {
                // Fallback: try deserializing the whole response
                var json = JsonSerializer.Serialize(rpcResponse.Response);
                deviceData = JsonSerializer.Deserialize<DailyEnergyFlowStateDto>(json);
            }
        }
        catch (JsonException)
        {
            // Return empty response if deserialization fails
            return new DailyEnergySummaryDto
            {
                DeviceLocalTime = DateTime.UtcNow,
                DeviceId = request.DeviceId,
                GridImport = 0.0,
                GridExport = 0.0,
                Consumption = 0.0,
                Production = 0.0
            };
        }

        if (deviceData == null)
        {
            // Return empty response if data is null
            return new DailyEnergySummaryDto
            {
                DeviceLocalTime = DateTime.UtcNow,
                DeviceId = request.DeviceId,
                GridImport = 0.0,
                GridExport = 0.0,
                Consumption = 0.0,
                Production = 0.0
            };
        }

        // Record device activity - device is online since RPC succeeded
        await _activityService.RecordActivityAsync(
            thingsBoardDeviceId.Value,
            TelemetrySource.DailyEnergyFlow,
            cancellationToken: cancellationToken);

        // Parse timestamp from device data
        DateTime dataDateTime = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(deviceData.TimestampUtc) &&
            DateTime.TryParse(deviceData.TimestampUtc, out var parsedTimestamp))
        {
            dataDateTime = parsedTimestamp;
        }

        // Map device data to response DTO
        // Consumption = Load + Battery Charge
        // Production = PV + Generator
        var result = new DailyEnergySummaryDto
        {
            DeviceLocalTime = dataDateTime,
            DeviceId = request.DeviceId,
            GridImport = deviceData.Grid?.ImportEnergyKWh ?? 0.0,
            GridExport = deviceData.Grid?.ExportEnergyKWh ?? 0.0,
            Consumption = (deviceData.LoadEnergyKWh ?? 0.0) + (deviceData.Battery?.ChargeEnergyKWh ?? 0.0),
            Production = (deviceData.Pv?.TotalEnergyKWh ?? 0.0) + (deviceData.Generator?.EnergyKWh ?? 0.0)
        };

        // Cache the result for 1 hour
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    /// <summary>
    /// Returns simulated daily energy summary for demo devices
    /// Values scale based on time of day (more energy generated/consumed as day progresses)
    /// </summary>
    private static DailyEnergySummaryDto GetSimulatedDailyEnergySummary(Guid deviceId)
    {
        var now = DateTime.UtcNow;
        var hour = now.Hour;
        var random = new Random(now.DayOfYear); // Consistent for the day

        // Scale factors based on time of day (energy accumulates throughout the day)
        var dayProgress = hour / 24.0;

        // Base daily values for a 5kW system
        var dailyProduction = 32.5 * GetSeasonalPvMultiplier(now.Month);
        var dailyConsumption = 18.0 * GetSeasonalLoadMultiplier(now.Month);

        // Calculate current values based on time of day
        // Production follows bell curve (peak at midday)
        var productionProgress = hour < 6 ? 0 :
            (hour > 20 ? 1.0 : Math.Sin((hour - 6) * Math.PI / 14));

        var currentProduction = dailyProduction * productionProgress * (0.9 + random.NextDouble() * 0.2);
        var currentConsumption = dailyConsumption * dayProgress * (0.9 + random.NextDouble() * 0.2);

        // Grid import/export based on net energy
        var netEnergy = currentProduction - currentConsumption;
        var gridExport = netEnergy > 0 ? netEnergy * 0.6 : 0;
        var gridImport = netEnergy < 0 ? Math.Abs(netEnergy) * 0.7 : 0;

        return new DailyEnergySummaryDto
        {
            DeviceLocalTime = now,
            DeviceId = deviceId,
            Production = Math.Round(currentProduction, 2),
            Consumption = Math.Round(currentConsumption, 2),
            GridExport = Math.Round(gridExport, 2),
            GridImport = Math.Round(gridImport, 2)
        };
    }

    private static double GetSeasonalPvMultiplier(int month) => month switch
    {
        1 => 0.25, 2 => 0.35, 3 => 0.55, 4 => 0.80, 5 => 1.00, 6 => 1.15,
        7 => 1.10, 8 => 1.00, 9 => 0.75, 10 => 0.50, 11 => 0.30, 12 => 0.20,
        _ => 1.0
    };

    private static double GetSeasonalLoadMultiplier(int month) => month switch
    {
        1 => 1.30, 2 => 1.25, 3 => 1.10, 4 => 0.95, 5 => 0.85, 6 => 0.80,
        7 => 0.82, 8 => 0.85, 9 => 0.95, 10 => 1.10, 11 => 1.25, 12 => 1.35,
        _ => 1.0
    };
}
