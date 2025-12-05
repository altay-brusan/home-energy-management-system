using Microsoft.AspNetCore.SignalR;

namespace Hems_Backend.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for pushing device heartbeat status changes to frontend
/// </summary>
public class DeviceStatusHub : Hub
{
    public async Task SubscribeToDevice(string deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }

    public async Task UnsubscribeFromDevice(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }
}
