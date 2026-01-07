using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for pushing device heartbeat status changes to frontend
/// Frontend uses local deviceId, backend translates to/from ThingsBoardDeviceId
/// </summary>
public class DeviceStatusHub : Hub
{
    private readonly IDeviceHeartbeatCache _heartbeatCache;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<DeviceStatusHub> _logger;

    public DeviceStatusHub(
        IDeviceHeartbeatCache heartbeatCache,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        ILogger<DeviceStatusHub> logger)
    {
        _heartbeatCache = heartbeatCache;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
        _logger = logger;
    }

    public async Task SubscribeToDevice(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out var localDeviceId))
        {
            _logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{localDeviceId}");

        // Check if demo device - always return ONLINE status
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(localDeviceId);
        if (isDemoDevice)
        {
            await Clients.Caller.SendAsync("DeviceStatusChanged", new
            {
                deviceId = localDeviceId,
                status = "ONLINE",
                missedHeartbeats = 0,
                timestamp = DateTime.UtcNow,
                isInitialStatus = true
            });
            return;
        }

        // ID ADAPTER: Frontend sends local deviceId, translate to ThingsBoard ID
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(localDeviceId);

        if (thingsBoardDeviceId == null)
        {
            _logger.LogWarning("Device mapping not found for deviceId: {DeviceId}", localDeviceId);
            return;
        }

        // Get full state using ThingsBoard ID (internal) - includes missedHeartbeats
        var deviceState = _heartbeatCache.GetDeviceState(thingsBoardDeviceId.Value);
        var currentStatus = deviceState?.Status ?? DeviceStatus.NOT_TRACKED;
        var missedHeartbeats = deviceState?.MissedHeartbeats ?? 0;
        var lastHeartbeat = deviceState?.LastHeartbeat;

        // Send response with local deviceId (frontend doesn't know about TB ID)
        await Clients.Caller.SendAsync("DeviceStatusChanged", new
        {
            deviceId = localDeviceId, // ← Local ID for frontend
            status = currentStatus.ToString(),
            missedHeartbeats = missedHeartbeats,
            timestamp = lastHeartbeat ?? DateTime.UtcNow,
            isInitialStatus = true
        });
    }

    public async Task UnsubscribeFromDevice(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out var localDeviceId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{localDeviceId}");
    }
}
