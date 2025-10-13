using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Hems_Backend.Web.Hubs;

[Authorize]
public class DeviceNotificationHub : Hub<IDeviceNotificationHubClient>
{
    public async Task JoinSiteGroup(Guid siteId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Site_{siteId}");
    }

    public async Task LeaveSiteGroup(Guid siteId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Site_{siteId}");
    }

    public async Task JoinDeviceGroup(Guid deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Device_{deviceId}");
    }

    public async Task LeaveDeviceGroup(Guid deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Device_{deviceId}");
    }
}

public interface IDeviceNotificationHubClient
{
    Task ReceiveDeviceNotification(DeviceNotificationDto notification);
    Task ReceiveDeviceNotificationUpdate(DeviceNotificationDto notification);
}
