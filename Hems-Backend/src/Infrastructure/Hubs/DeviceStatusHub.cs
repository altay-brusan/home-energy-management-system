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
    private readonly ILogger<DeviceStatusHub> _logger;

    public DeviceStatusHub(
        IDeviceHeartbeatCache heartbeatCache,
        IDeviceMappingService deviceMapping,
        ILogger<DeviceStatusHub> logger)
    {
        _heartbeatCache = heartbeatCache;
        _deviceMapping = deviceMapping;
        _logger = logger;
    }

    public async Task SubscribeToDevice(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out var localDeviceId))
        {
            _logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceId);
            return;
        }

        // ID ADAPTER: Frontend sends local deviceId, translate to ThingsBoard ID
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(localDeviceId);
        
        if (thingsBoardDeviceId == null)
        {
            _logger.LogWarning("Device mapping not found for deviceId: {DeviceId}", localDeviceId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{localDeviceId}");
        
        // Get status using ThingsBoard ID (internal)
        var currentStatus = _heartbeatCache.GetDeviceStatus(thingsBoardDeviceId.Value);
        
        // Send response with local deviceId (frontend doesn't know about TB ID)
        await Clients.Caller.SendAsync("DeviceStatusChanged", new
        {
            deviceId = localDeviceId, // ← Local ID for frontend
            status = currentStatus.ToString(),
            missedHeartbeats = 0,
            timestamp = DateTime.UtcNow,
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
