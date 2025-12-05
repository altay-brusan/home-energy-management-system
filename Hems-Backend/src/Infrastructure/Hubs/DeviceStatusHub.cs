using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Hems_Backend.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for pushing device heartbeat status changes to frontend
/// </summary>
public class DeviceStatusHub : Hub
{
    private readonly IDeviceHeartbeatCache _heartbeatCache;

    public DeviceStatusHub(IDeviceHeartbeatCache heartbeatCache)
    {
        _heartbeatCache = heartbeatCache;
    }

    public async Task SubscribeToDevice(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out var deviceGuid))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        
        // Send initial status immediately after subscription
        var currentStatus = _heartbeatCache.GetDeviceStatus(deviceGuid);
        await Clients.Caller.SendAsync("DeviceStatusChanged", new
        {
            deviceId = deviceGuid,
            status = currentStatus.ToString(),
            missedHeartbeats = 0, // We don't expose internal count on initial load
            timestamp = DateTime.UtcNow,
            isInitialStatus = true
        });
    }

    public async Task UnsubscribeFromDevice(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }
}
