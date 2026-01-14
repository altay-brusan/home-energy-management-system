using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for pushing device heartbeat status changes to frontend.
/// Frontend uses local deviceId, backend translates to/from ThingsBoardDeviceId.
/// Includes authorization, access validation, and connection tracking.
///
/// NOTE: This hub is in Infrastructure (not Web) because Infrastructure services
/// (DeviceHeartbeatMonitorService, DeviceActivityService) need to use IHubContext&lt;DeviceStatusHub&gt;
/// to send SignalR notifications. Moving it to Web would create a circular dependency.
/// </summary>
[Authorize]
public class DeviceStatusHub : Hub
{
    private readonly IDeviceHeartbeatCache _heartbeatCache;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDemoDataService _demoDataService;
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ILogger<DeviceStatusHub> _logger;

    // Track subscriptions per connection to prevent duplicates and enable cleanup
    private static readonly ConcurrentDictionary<string, HashSet<Guid>> _connectionSubscriptions = new();

    public DeviceStatusHub(
        IDeviceHeartbeatCache heartbeatCache,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        IApplicationDbContext context,
        IUser user,
        ILogger<DeviceStatusHub> logger)
    {
        _heartbeatCache = heartbeatCache;
        _deviceMapping = deviceMapping;
        _demoDataService = demoDataService;
        _context = context;
        _user = user;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _connectionSubscriptions[Context.ConnectionId] = new HashSet<Guid>();
        _logger.LogInformation(
            "DeviceStatusHub - Client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, GetUserIdFromContext());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up all subscriptions for this connection
        if (_connectionSubscriptions.TryRemove(Context.ConnectionId, out var deviceIds))
        {
            foreach (var deviceId in deviceIds)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
            }

            _logger.LogInformation(
                "DeviceStatusHub - Client disconnected: {ConnectionId}, cleaned up {Count} subscriptions. Exception: {Exception}",
                Context.ConnectionId, deviceIds.Count, exception?.Message ?? "None");
        }
        else
        {
            _logger.LogInformation(
                "DeviceStatusHub - Client disconnected: {ConnectionId}. Exception: {Exception}",
                Context.ConnectionId, exception?.Message ?? "None");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToDevice(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out var localDeviceId))
        {
            _logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceId);
            await SendErrorAsync("InvalidDeviceId", $"Invalid device ID format: {deviceId}");
            return;
        }

        // Check if already subscribed to prevent duplicate processing
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subscriptions))
        {
            if (subscriptions.Contains(localDeviceId))
            {
                _logger.LogDebug(
                    "DeviceStatusHub - Already subscribed to device {DeviceId}, skipping",
                    localDeviceId);
                // Still send current status even if already subscribed
                await SendCurrentStatusAsync(localDeviceId);
                return;
            }
        }

        // Validate access to the device
        try
        {
            await ValidateDeviceAccessAsync(localDeviceId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "DeviceStatusHub - Access denied for device {DeviceId}: {Message}",
                localDeviceId, ex.Message);
            await SendErrorAsync("AccessDenied", ex.Message);
            return;
        }

        // Add to group and track subscription
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{localDeviceId}");

        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs)
            {
                subs.Add(localDeviceId);
            }
        }

        _logger.LogInformation(
            "DeviceStatusHub - Subscribed to device {DeviceId}, connection: {ConnectionId}",
            localDeviceId, Context.ConnectionId);

        // Send current status
        await SendCurrentStatusAsync(localDeviceId);
    }

    public async Task UnsubscribeFromDevice(string deviceId)
    {
        if (!Guid.TryParse(deviceId, out var localDeviceId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{localDeviceId}");

        // Remove from tracking
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs)
            {
                subs.Remove(localDeviceId);
            }
        }

        _logger.LogInformation(
            "DeviceStatusHub - Unsubscribed from device {DeviceId}, connection: {ConnectionId}",
            localDeviceId, Context.ConnectionId);
    }

    /// <summary>
    /// Get the list of devices this connection is subscribed to
    /// </summary>
    public Task<List<Guid>> GetSubscribedDevices()
    {
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs)
            {
                return Task.FromResult(subs.ToList());
            }
        }
        return Task.FromResult(new List<Guid>());
    }

    private async Task SendCurrentStatusAsync(Guid localDeviceId)
    {
        // Check if demo device - always return ONLINE status
        var isDemoDevice = await _demoDataService.IsDemoDeviceAsync(localDeviceId);

        _logger.LogInformation(
            "DeviceStatusHub - SendCurrentStatusAsync: DeviceId={DeviceId}, IsDemoDevice={IsDemoDevice}",
            localDeviceId, isDemoDevice);

        if (isDemoDevice)
        {
            _logger.LogInformation(
                "DeviceStatusHub - Returning OK status for demo device {DeviceId}",
                localDeviceId);

            // IMPORTANT: Send "OK" not "ONLINE" - mobile app expects DeviceStatusType enum values
            // DeviceStatusType = 'OK' | 'PENDING' | 'WAITING' | 'DISCONNECTED' | 'NOT_TRACKED'
            await Clients.Caller.SendAsync("DeviceStatusChanged", new
            {
                deviceId = localDeviceId,
                status = "OK",
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
            await SendErrorAsync("DeviceMappingNotFound",
                $"Device mapping not found for device {localDeviceId}. Device may not be configured.");
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
            deviceId = localDeviceId,
            status = currentStatus.ToString(),
            missedHeartbeats = missedHeartbeats,
            timestamp = lastHeartbeat ?? DateTime.UtcNow,
            isInitialStatus = true
        });
    }

    private async Task SendErrorAsync(string errorCode, string message)
    {
        await Clients.Caller.SendAsync("DeviceStatusError", new
        {
            errorCode = errorCode,
            message = message,
            timestamp = DateTime.UtcNow
        });
    }

    private async Task ValidateDeviceAccessAsync(Guid deviceId)
    {
        var userId = GetUserIdFromContext();

        // Check if user is Admin - Admins can access all devices
        if (Context.User?.IsInRole("Administrator") == true)
        {
            _logger.LogDebug("DeviceStatusHub - Admin user {UserId} granted access to device {DeviceId}", userId, deviceId);
            return;
        }

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
}
