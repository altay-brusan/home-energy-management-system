using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Web.Hubs;

[Authorize]
public class DeviceNotificationHub : Hub<IDeviceNotificationHubClient>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeviceNotificationHub(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task JoinSiteGroup(Guid siteId)
    {
        // ✅ Site erişim yetkisi kontrolü
        await ValidateSiteAccessAsync(siteId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Site_{siteId}");
    }

    public async Task LeaveSiteGroup(Guid siteId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Site_{siteId}");
    }

    public async Task JoinDeviceGroup(Guid deviceId)
    {
        // ✅ Device erişim yetkisi kontrolü
        await ValidateDeviceAccessAsync(deviceId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Device_{deviceId}");
    }

    public async Task LeaveDeviceGroup(Guid deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Device_{deviceId}");
    }

    private async Task ValidateSiteAccessAsync(Guid siteId)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Site sahibi mi kontrol et
        var isOwner = await _context.Sites
            .AnyAsync(s => s.Id == siteId && s.UserId == userId);

        if (isOwner) return;

        // UserSite tablosunda erişim var mı kontrol et
        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == siteId && us.UserId == userId);

        if (!hasUserSiteAccess)
        {
            throw new UnauthorizedAccessException($"You don't have access to site {siteId}");
        }
    }

    private async Task ValidateDeviceAccessAsync(Guid deviceId)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        // Device'ın hangi site'a ait olduğunu bul
        var deviceSite = await _context.SetupDevices
            .Where(sd => sd.DeviceId == deviceId)
            .Select(sd => new { sd.Setup.SiteId, sd.Setup.Site.UserId })
            .FirstOrDefaultAsync();

        if (deviceSite == null)
        {
            throw new UnauthorizedAccessException($"Device {deviceId} not found");
        }

        // Site sahibi mi kontrol et
        if (deviceSite.UserId == userId) return;

        // UserSite tablosunda erişim var mı kontrol et
        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == deviceSite.SiteId && us.UserId == userId);

        if (!hasUserSiteAccess)
        {
            throw new UnauthorizedAccessException($"You don't have access to device {deviceId}");
        }
    }
}

public interface IDeviceNotificationHubClient
{
    Task ReceiveDeviceNotification(DeviceNotificationDto notification);
    Task ReceiveDeviceNotificationUpdate(DeviceNotificationDto notification);
}
