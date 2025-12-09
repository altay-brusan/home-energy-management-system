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

    public GetDailyEnergySummaryHandler(
        ISender sender,
        IDeviceMappingService deviceMapping,
        IThingsBoardConnectorService tbService,
        IMemoryCache cache)
    {
        _sender = sender;
        _deviceMapping = deviceMapping;
        _tbService = tbService;
        _cache = cache;
    }

    public async Task<DailyEnergySummaryDto> Handle(GetDailyEnergySummaryQuery request, CancellationToken cancellationToken)
    {
        // Device access check (uses local DeviceId)
        var hasAccess = await _sender.Send(new CheckDeviceAccessQuery { DeviceId = request.DeviceId }, cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Device access denied");
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
                DataDateTime = DateTime.UtcNow,
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
            var json = JsonSerializer.Serialize(rpcResponse.Response);
            deviceData = JsonSerializer.Deserialize<DailyEnergyFlowStateDto>(json);
        }
        catch (JsonException)
        {
            // Return empty response if deserialization fails
            return new DailyEnergySummaryDto
            {
                DataDateTime = DateTime.UtcNow,
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
                DataDateTime = DateTime.UtcNow,
                DeviceId = request.DeviceId,
                GridImport = 0.0,
                GridExport = 0.0,
                Consumption = 0.0,
                Production = 0.0
            };
        }

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
            DataDateTime = dataDateTime,
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
}
