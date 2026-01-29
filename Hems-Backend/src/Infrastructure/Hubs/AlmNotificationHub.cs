using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for pushing ALM prediction notifications to frontend.
/// Clients subscribe to device-level groups to receive prediction results.
/// </summary>
[Authorize]
public class AlmNotificationHub : Hub<IAlmNotificationHubClient>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ILogger<AlmNotificationHub> _logger;

    private static readonly ConcurrentDictionary<string, HashSet<Guid>> _connectionDevices = new();

    public AlmNotificationHub(
        IApplicationDbContext context,
        IUser user,
        ILogger<AlmNotificationHub> logger)
    {
        _context = context;
        _user = user;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _connectionDevices[Context.ConnectionId] = new HashSet<Guid>();
        _logger.LogInformation("AlmNotificationHub - Client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, GetUserIdFromContext());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionDevices.TryRemove(Context.ConnectionId, out var deviceIds))
        {
            foreach (var deviceId in deviceIds)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"AlmDevice_{deviceId}");
            }

            _logger.LogInformation(
                "AlmNotificationHub - Client disconnected: {ConnectionId}, cleaned up {Count} device subscriptions",
                Context.ConnectionId, deviceIds.Count);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToDevice(Guid deviceId)
    {
        if (_connectionDevices.TryGetValue(Context.ConnectionId, out var devices) && devices.Contains(deviceId))
        {
            return;
        }

        await ValidateDeviceAccessAsync(deviceId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"AlmDevice_{deviceId}");

        if (_connectionDevices.TryGetValue(Context.ConnectionId, out var devIds))
        {
            lock (devIds)
            {
                devIds.Add(deviceId);
            }
        }

        _logger.LogInformation("AlmNotificationHub - Subscribed to device {DeviceId}, connection: {ConnectionId}",
            deviceId, Context.ConnectionId);
    }

    public async Task UnsubscribeFromDevice(Guid deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"AlmDevice_{deviceId}");

        if (_connectionDevices.TryGetValue(Context.ConnectionId, out var devIds))
        {
            lock (devIds)
            {
                devIds.Remove(deviceId);
            }
        }

        _logger.LogInformation("AlmNotificationHub - Unsubscribed from device {DeviceId}, connection: {ConnectionId}",
            deviceId, Context.ConnectionId);
    }

    private string GetUserIdFromContext()
    {
        string? userId = null;

        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value
                    ?? Context.User?.FindFirst("unique_name")?.Value
                    ?? Context.User?.FindFirst("email")?.Value
                    ?? Context.User?.Identity?.Name;
        }

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

        if (Context.User?.IsInRole("Administrator") == true) return;

        var deviceSite = await _context.SetupDevices
            .Where(sd => sd.DeviceId == deviceId)
            .Select(sd => new { sd.Setup.SiteId, sd.Setup.Site.UserId })
            .FirstOrDefaultAsync();

        if (deviceSite == null)
        {
            throw new UnauthorizedAccessException($"Device {deviceId} not found or not assigned to any site");
        }

        if (deviceSite.UserId == userId) return;

        var hasUserSiteAccess = await _context.UserSites
            .AnyAsync(us => us.SiteId == deviceSite.SiteId && us.UserId == userId);

        if (!hasUserSiteAccess)
        {
            throw new UnauthorizedAccessException($"You don't have access to device {deviceId}");
        }
    }
}

public interface IAlmNotificationHubClient
{
    /// <summary>
    /// Notifies when a prediction job has been submitted/started
    /// </summary>
    Task PredictionStarted(AlmPredictionNotification notification);

    /// <summary>
    /// Notifies when a prediction job has completed successfully
    /// </summary>
    Task PredictionCompleted(AlmPredictionNotification notification);

    /// <summary>
    /// Notifies when a prediction job has failed
    /// </summary>
    Task PredictionFailed(AlmPredictionNotification notification);

    /// <summary>
    /// Notifies about ALM service health status changes
    /// </summary>
    Task AlmHealthStatus(AlmHealthStatusNotification notification);
}

public class AlmPredictionNotification
{
    public Guid JobId { get; set; }
    public Guid DeviceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public string? AlmJobId { get; set; }
    public bool FromCache { get; set; }
    public int PredictionCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class AlmHealthStatusNotification
{
    public bool IsHealthy { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
