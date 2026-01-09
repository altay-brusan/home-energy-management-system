using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Hems_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Web.Hubs;

/// <summary>
/// SignalR hub for streaming PowerFlow data to frontend.
/// Uses shared stream manager to optimize multiple connections to same device.
/// Includes authorization, access validation, and connection tracking.
/// </summary>
[Authorize]
public class PowerFlowHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IPowerFlowService _powerFlowService;
    private readonly ISharedDeviceStreamManager _streamManager;
    private readonly ILogger<PowerFlowHub> _logger;

    // Track active streams per connection for cleanup
    private static readonly ConcurrentDictionary<string, HashSet<string>> _connectionStreams = new();

    public PowerFlowHub(
        IApplicationDbContext context,
        IUser user,
        IPowerFlowService powerFlowService,
        ISharedDeviceStreamManager streamManager,
        ILogger<PowerFlowHub> logger)
    {
        _context = context;
        _user = user;
        _powerFlowService = powerFlowService;
        _streamManager = streamManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _connectionStreams[Context.ConnectionId] = new HashSet<string>();
        _logger.LogInformation(
            "PowerFlowHub - Client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, GetUserIdFromContext());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionStreams.TryRemove(Context.ConnectionId, out var streamIds))
        {
            _logger.LogInformation(
                "PowerFlowHub - Client disconnected: {ConnectionId}, had {Count} active streams. Exception: {Exception}",
                Context.ConnectionId, streamIds.Count, exception?.Message ?? "None");
        }
        else
        {
            _logger.LogInformation(
                "PowerFlowHub - Client disconnected: {ConnectionId}. Exception: {Exception}",
                Context.ConnectionId, exception?.Message ?? "None");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// React Native will call this as a streaming method:
    /// connection.stream("StreamPowerFlow", deviceId)
    ///
    /// Uses shared stream manager to ensure only one polling loop per device.
    /// Multiple subscribers share the same data stream.
    /// When the client disconnects, the CancellationToken is canceled and the subscription is removed.
    /// </summary>
    public async IAsyncEnumerable<PowerFlowDto> StreamPowerFlow(
        Guid deviceId,
        int intervalSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PowerFlowHub - StreamPowerFlow started for device {DeviceId}, interval: {Interval}s, connection: {ConnectionId}",
            deviceId, intervalSeconds, Context.ConnectionId);

        // Validate access to the device
        try
        {
            await ValidateDeviceAccessAsync(deviceId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "PowerFlowHub - Access denied for device {DeviceId}: {Message}",
                deviceId, ex.Message);
            // Send error and exit
            await Clients.Caller.SendAsync("PowerFlowError", new
            {
                errorCode = "AccessDenied",
                message = ex.Message,
                deviceId = deviceId,
                timestamp = DateTime.UtcNow
            }, cancellationToken);
            yield break;
        }

        // Subscribe to shared stream (creates new stream if first subscriber, reuses if exists)
        IDeviceStreamSubscription? subscription = null;
        try
        {
            subscription = await _streamManager.SubscribeAsync(deviceId, intervalSeconds, cancellationToken);

            // Track this stream for the connection
            if (_connectionStreams.TryGetValue(Context.ConnectionId, out var streams))
            {
                lock (streams)
                {
                    streams.Add(subscription.SubscriptionId);
                }
            }

            _logger.LogInformation(
                "PowerFlowHub - Subscribed to device {DeviceId}, subscriptionId: {SubscriptionId}, total subscribers: {Count}",
                deviceId, subscription.SubscriptionId, _streamManager.GetSubscriberCount(deviceId));

            // Read from the shared channel
            await foreach (var data in subscription.DataChannel.ReadAllAsync(cancellationToken))
            {
                if (data is PowerFlowDto powerFlowDto)
                {
                    yield return powerFlowDto;
                }
            }
        }
        finally
        {
            // Ensure we unsubscribe when done
            if (subscription != null)
            {
                // Remove from tracking
                if (_connectionStreams.TryGetValue(Context.ConnectionId, out var streams))
                {
                    lock (streams)
                    {
                        streams.Remove(subscription.SubscriptionId);
                    }
                }

                await subscription.DisposeAsync();
                _logger.LogInformation(
                    "PowerFlowHub - Unsubscribed from device {DeviceId}, subscriptionId: {SubscriptionId}, remaining subscribers: {Count}",
                    deviceId, subscription.SubscriptionId, _streamManager.GetSubscriberCount(deviceId));
            }
        }
    }

    /// <summary>
    /// Legacy method for backwards compatibility.
    /// Directly polls the device without using shared stream.
    /// Use StreamPowerFlow instead for better performance with multiple clients.
    /// </summary>
    public async IAsyncEnumerable<PowerFlowDto> StreamPowerFlowDirect(
        Guid deviceId,
        int intervalSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PowerFlowHub - StreamPowerFlowDirect (legacy) started for device {DeviceId}, interval: {Interval}s",
            deviceId, intervalSeconds);

        // Validate access to the device
        try
        {
            await ValidateDeviceAccessAsync(deviceId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "PowerFlowHub - Access denied for device {DeviceId}: {Message}",
                deviceId, ex.Message);
            await Clients.Caller.SendAsync("PowerFlowError", new
            {
                errorCode = "AccessDenied",
                message = ex.Message,
                deviceId = deviceId,
                timestamp = DateTime.UtcNow
            }, cancellationToken);
            yield break;
        }

        // Clamp interval to a sane range
        if (intervalSeconds <= 0) intervalSeconds = 5;
        if (intervalSeconds < 2) intervalSeconds = 2;
        if (intervalSeconds > 60) intervalSeconds = 60;

        var interval = TimeSpan.FromSeconds(intervalSeconds);

        while (!cancellationToken.IsCancellationRequested)
        {
            PowerFlowDto? dto = null;
            try
            {
                dto = await _powerFlowService.GetPowerFlowAsync(deviceId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("PowerFlowHub - Cancelled for device {DeviceId}", deviceId);
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PowerFlowHub - error getting PowerFlow for device {DeviceId}", deviceId);
                await Clients.Caller.SendAsync("PowerFlowError", new
                {
                    errorCode = "FetchError",
                    message = "Failed to fetch PowerFlow data from device",
                    deviceId = deviceId,
                    timestamp = DateTime.UtcNow
                }, cancellationToken);
                yield break;
            }

            if (dto != null)
            {
                yield return dto;
            }

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Get current stream statistics for monitoring/debugging
    /// </summary>
    public SharedStreamStatistics GetStreamStatistics()
    {
        return _streamManager.GetStatistics();
    }

    /// <summary>
    /// Get the number of active streams for this connection
    /// </summary>
    public int GetActiveStreamCount()
    {
        if (_connectionStreams.TryGetValue(Context.ConnectionId, out var streams))
        {
            lock (streams)
            {
                return streams.Count;
            }
        }
        return 0;
    }

    #region Authorization helpers

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

    #endregion
}
