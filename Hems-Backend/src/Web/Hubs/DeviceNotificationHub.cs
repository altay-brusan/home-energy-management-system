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
        var userId = GetUserIdFromContext();

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

    private string GetUserIdFromContext()
    {
        string? userId = null;
        
        // Try Context.User first (SignalR Hub context)
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value // JWT sub claim
                    ?? Context.User?.FindFirst("unique_name")?.Value // JWT unique_name
                    ?? Context.User?.FindFirst("email")?.Value // JWT email
                    ?? Context.User?.Identity?.Name; // Generic identity name
        }
        
        // If Context.User failed, try _user service (IUser) 
        if (string.IsNullOrEmpty(userId))
        {
            userId = _user.Id;
        }
        
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated. Context.User is null and _user.Id is also null.");
        }
        
        return userId;
    }

    private async Task ValidateDeviceAccessAsync(Guid deviceId)
    {
        var userId = GetUserIdFromContext();

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
