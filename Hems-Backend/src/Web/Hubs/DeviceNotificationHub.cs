using System.Collections.Concurrent;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Web.Hubs;

/// <summary>
/// SignalR hub for pushing device notifications to frontend.
/// Supports site-level and device-level group subscriptions.
/// Includes authorization, access validation, and connection tracking.
/// </summary>
[Authorize]
public class DeviceNotificationHub : Hub<IDeviceNotificationHubClient>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ILogger<DeviceNotificationHub> _logger;

    // Track subscriptions per connection: ConnectionId -> (SiteIds, DeviceIds)
    private static readonly ConcurrentDictionary<string, ConnectionSubscriptions> _connectionSubscriptions = new();

    public DeviceNotificationHub(
        IApplicationDbContext context,
        IUser user,
        ILogger<DeviceNotificationHub> logger)
    {
        _context = context;
        _user = user;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _connectionSubscriptions[Context.ConnectionId] = new ConnectionSubscriptions();
        _logger.LogInformation(
            "DeviceNotificationHub - Client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, GetUserIdFromContext());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionSubscriptions.TryRemove(Context.ConnectionId, out var subscriptions))
        {
            // Clean up site subscriptions
            foreach (var siteId in subscriptions.SiteIds)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Site_{siteId}");
            }

            // Clean up device subscriptions
            foreach (var deviceId in subscriptions.DeviceIds)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Device_{deviceId}");
            }

            _logger.LogInformation(
                "DeviceNotificationHub - Client disconnected: {ConnectionId}, cleaned up {SiteCount} site and {DeviceCount} device subscriptions. Exception: {Exception}",
                Context.ConnectionId, subscriptions.SiteIds.Count, subscriptions.DeviceIds.Count, exception?.Message ?? "None");
        }
        else
        {
            _logger.LogInformation(
                "DeviceNotificationHub - Client disconnected: {ConnectionId}. Exception: {Exception}",
                Context.ConnectionId, exception?.Message ?? "None");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSiteGroup(Guid siteId)
    {
        // Check if already subscribed
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subscriptions))
        {
            if (subscriptions.SiteIds.Contains(siteId))
            {
                _logger.LogDebug(
                    "DeviceNotificationHub - Already subscribed to site {SiteId}, skipping",
                    siteId);
                return;
            }
        }

        // Validate site access
        try
        {
            await ValidateSiteAccessAsync(siteId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "DeviceNotificationHub - Access denied for site {SiteId}: {Message}",
                siteId, ex.Message);
            await SendErrorAsync("AccessDenied", ex.Message);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Site_{siteId}");

        // Track subscription
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs.SiteIds)
            {
                subs.SiteIds.Add(siteId);
            }
        }

        _logger.LogInformation(
            "DeviceNotificationHub - Joined site group {SiteId}, connection: {ConnectionId}",
            siteId, Context.ConnectionId);
    }

    public async Task LeaveSiteGroup(Guid siteId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Site_{siteId}");

        // Remove from tracking
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs.SiteIds)
            {
                subs.SiteIds.Remove(siteId);
            }
        }

        _logger.LogInformation(
            "DeviceNotificationHub - Left site group {SiteId}, connection: {ConnectionId}",
            siteId, Context.ConnectionId);
    }

    public async Task JoinDeviceGroup(Guid deviceId)
    {
        // Check if already subscribed
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subscriptions))
        {
            if (subscriptions.DeviceIds.Contains(deviceId))
            {
                _logger.LogDebug(
                    "DeviceNotificationHub - Already subscribed to device {DeviceId}, skipping",
                    deviceId);
                return;
            }
        }

        // Validate device access
        try
        {
            await ValidateDeviceAccessAsync(deviceId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "DeviceNotificationHub - Access denied for device {DeviceId}: {Message}",
                deviceId, ex.Message);
            await SendErrorAsync("AccessDenied", ex.Message);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Device_{deviceId}");

        // Track subscription
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs.DeviceIds)
            {
                subs.DeviceIds.Add(deviceId);
            }
        }

        _logger.LogInformation(
            "DeviceNotificationHub - Joined device group {DeviceId}, connection: {ConnectionId}",
            deviceId, Context.ConnectionId);
    }

    public async Task LeaveDeviceGroup(Guid deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Device_{deviceId}");

        // Remove from tracking
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs.DeviceIds)
            {
                subs.DeviceIds.Remove(deviceId);
            }
        }

        _logger.LogInformation(
            "DeviceNotificationHub - Left device group {DeviceId}, connection: {ConnectionId}",
            deviceId, Context.ConnectionId);
    }

    /// <summary>
    /// Get the list of sites and devices this connection is subscribed to
    /// </summary>
    public Task<object> GetSubscriptions()
    {
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs.SiteIds)
            {
                lock (subs.DeviceIds)
                {
                    return Task.FromResult<object>(new
                    {
                        siteIds = subs.SiteIds.ToList(),
                        deviceIds = subs.DeviceIds.ToList()
                    });
                }
            }
        }
        return Task.FromResult<object>(new { siteIds = new List<Guid>(), deviceIds = new List<Guid>() });
    }

    private async Task SendErrorAsync(string errorCode, string message)
    {
        await Clients.Caller.NotificationError(new
        {
            errorCode = errorCode,
            message = message,
            timestamp = DateTime.UtcNow
        });
    }

    private async Task ValidateSiteAccessAsync(Guid siteId)
    {
        var userId = GetUserIdFromContext();

        // Check if user is site owner
        var isOwner = await _context.Sites
            .AnyAsync(s => s.Id == siteId && s.UserId == userId);

        if (isOwner) return;

        // Check if user has shared access via UserSites table
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
                    ?? Context.User?.FindFirst("sub")?.Value
                    ?? Context.User?.FindFirst("unique_name")?.Value
                    ?? Context.User?.FindFirst("email")?.Value
                    ?? Context.User?.Identity?.Name;
        }

        // If Context.User failed, try _user service (IUser)
        if (string.IsNullOrEmpty(userId))
        {
            userId = _user.Id;
        }

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        return userId;
    }

    private async Task ValidateDeviceAccessAsync(Guid deviceId)
    {
        var userId = GetUserIdFromContext();

        // Find which site the device belongs to
        var deviceSite = await _context.SetupDevices
            .Where(sd => sd.DeviceId == deviceId)
            .Select(sd => new { sd.Setup.SiteId, sd.Setup.Site.UserId })
            .FirstOrDefaultAsync();

        if (deviceSite == null)
        {
            throw new UnauthorizedAccessException($"Device {deviceId} not found or not assigned to any site");
        }

        // Check if user is site owner
        if (deviceSite.UserId == userId) return;

        // Check if user has shared access via UserSites table
        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == deviceSite.SiteId && us.UserId == userId);

        if (!hasUserSiteAccess)
        {
            throw new UnauthorizedAccessException($"You don't have access to device {deviceId}");
        }
    }

    /// <summary>
    /// Tracks site and device subscriptions for a connection
    /// </summary>
    private class ConnectionSubscriptions
    {
        public HashSet<Guid> SiteIds { get; } = new();
        public HashSet<Guid> DeviceIds { get; } = new();
    }
}

public interface IDeviceNotificationHubClient
{
    Task ReceiveDeviceNotification(DeviceNotificationDto notification);
    Task ReceiveDeviceNotificationUpdate(DeviceNotificationDto notification);
    Task NotificationError(object error);
}
