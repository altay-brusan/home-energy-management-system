using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Hems_Backend.Web.Services;

public interface IDeviceNotificationService
{
    Task SendNotificationToSiteAsync(Guid siteId, DeviceNotificationDto notification);
    Task SendNotificationToDeviceAsync(Guid deviceId, DeviceNotificationDto notification);
    Task SendNotificationUpdateAsync(Guid siteId, DeviceNotificationDto notification);
}

public class DeviceNotificationService : IDeviceNotificationService
{
    private readonly IHubContext<DeviceNotificationHub, IDeviceNotificationHubClient> _hubContext;

    public DeviceNotificationService(IHubContext<DeviceNotificationHub, IDeviceNotificationHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationToSiteAsync(Guid siteId, DeviceNotificationDto notification)
    {
        await _hubContext.Clients.Group($"Site_{siteId}").ReceiveDeviceNotification(notification);
    }

    public async Task SendNotificationToDeviceAsync(Guid deviceId, DeviceNotificationDto notification)
    {
        await _hubContext.Clients.Group($"Device_{deviceId}").ReceiveDeviceNotification(notification);
    }

    public async Task SendNotificationUpdateAsync(Guid siteId, DeviceNotificationDto notification)
    {
        await _hubContext.Clients.Group($"Site_{siteId}").ReceiveDeviceNotificationUpdate(notification);
    }
}
