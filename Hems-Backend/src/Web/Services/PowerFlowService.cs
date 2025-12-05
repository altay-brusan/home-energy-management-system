using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Hems_Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Web.Services;

public interface IPowerFlowService
{
    /// <summary>
    /// Calls ThingsBoard (via RPC) and returns the latest PowerFlow data for a device.
    /// </summary>
    Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, CancellationToken cancellationToken = default);
}

public class PowerFlowService : IPowerFlowService
{
    private readonly IThingsBoardConnectorService _tbConnector;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDeviceHeartbeatCache _heartbeatCache;
    private readonly IHubContext<DeviceStatusHub> _hubContext;
    private readonly ILogger<PowerFlowService> _logger;

    public PowerFlowService(
        IThingsBoardConnectorService tbConnector,
        IDeviceMappingService deviceMapping,
        IDeviceHeartbeatCache heartbeatCache,
        IHubContext<DeviceStatusHub> hubContext,
        ILogger<PowerFlowService> logger)
    {
        _tbConnector = tbConnector;
        _deviceMapping = deviceMapping;
        _heartbeatCache = heartbeatCache;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        // Map local deviceId to ThingsBoardDeviceId (with caching)
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(deviceId, cancellationToken);
        
        if (thingsBoardDeviceId == null)
        {
            _logger.LogWarning("Device mapping not found for deviceId: {DeviceId}", deviceId);
            return null;
        }

        // Use centralized ThingsBoard connector for RPC call
        var rpcResponse = await _tbConnector.SendTwoWayRpcAsync(
            thingsBoardDeviceId.Value,
            "getPowerFlow",
            new Dictionary<string, object>(),
            timeoutMs: 10000,
            cancellationToken);

        if (rpcResponse?.Success != true || rpcResponse.Response == null)
        {
            _logger.LogWarning("Failed to get PowerFlow from device {DeviceId}. Error: {Error}", 
                deviceId, rpcResponse?.Error);
            return null;
        }

        // Deserialize response to PowerFlowStateRpcDto
        var json = JsonSerializer.Serialize(rpcResponse.Response);
        var rpcState = JsonSerializer.Deserialize<PowerFlowStateRpcDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (rpcState == null) return null;

        // Record PowerFlow received - resets missed heartbeat count
        var statusChange = await _heartbeatCache.RecordPowerFlowAsync(thingsBoardDeviceId.Value, DateTime.UtcNow, cancellationToken);
        
        // If status changed (e.g., recovery from DISCONNECTED), send immediate SignalR notification
        if (statusChange != null)
        {
            await _hubContext.Clients.Group($"device_{deviceId}")
                .SendAsync("DeviceStatusChanged", new
                {
                    deviceId = deviceId, // Local ID for frontend
                    status = statusChange.NewStatus.ToString(),
                    missedHeartbeats = statusChange.MissedHeartbeats,
                    timestamp = statusChange.Timestamp
                }, cancellationToken);
            
            _logger.LogInformation("Immediate SignalR notification sent for device {DeviceId} recovery via PowerFlow: {Status}",
                deviceId, statusChange.NewStatus);
        }

        return MapToPowerFlowDto(deviceId, rpcState);
    }

    private static PowerFlowDto MapToPowerFlowDto(Guid deviceId, PowerFlowStateRpcDto state)
    {
        // safe helpers
        static double ToKw(int? w) => w.HasValue ? w.Value / 1000.0 : 0.0;

        return new PowerFlowDto
        {
            DeviceId = deviceId,
            TimestampUtc = state.TimestampUtc == default
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(state.TimestampUtc, TimeSpan.Zero),

            SolarPowerKw = ToKw(state.Pv?.TotalPowerW),
            GridPowerKw = ToKw(state.Grid?.TotalPowerW),
            LoadPowerKw = ToKw(state.Load?.TotalPowerW),
            BatteryPowerKw = ToKw(state.Battery?.PowerW),
            GeneratorPowerKw = ToKw(state.Generator?.TotalPowerW),

            RawJson = null // you can fill this if you need debugging
        };
    }

    #region Internal DTOs to match Node-RED PowerFlowState JSON

    private sealed class PowerFlowStateRpcDto
    {
        public string? Kind { get; init; }
        public DateTime TimestampUtc { get; init; }
        public int Version { get; init; }
        public string? DeviceId { get; init; }
        public PvDto? Pv { get; init; }
        public GridDto? Grid { get; init; }
        public LoadDto? Load { get; init; }
        public BatteryDto? Battery { get; init; }
        public GeneratorDto? Generator { get; init; }
    }

    private sealed class PvDto
    {
        public int? TotalPowerW { get; init; }
        public List<MpptDto>? Mppt { get; init; }
    }

    private sealed class MpptDto
    {
        public int Id { get; init; }
        public int? PowerW { get; init; }
    }

    private sealed class GridDto
    {
        public int? TotalPowerW { get; init; }
    }

    private sealed class LoadDto
    {
        public int? TotalPowerW { get; init; }
    }

    private sealed class BatteryDto
    {
        public int? PowerW { get; init; }
    }

    private sealed class GeneratorDto
    {
        public int? TotalPowerW { get; init; }
    }

    #endregion
}


