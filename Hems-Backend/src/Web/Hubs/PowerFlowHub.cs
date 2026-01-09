using System.Runtime.CompilerServices;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Hems_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Web.Hubs;

[Authorize]
public class PowerFlowHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IPowerFlowService _powerFlowService;
    private readonly ISharedDeviceStreamManager _streamManager;
    private readonly ILogger<PowerFlowHub> _logger;

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

        // Subscribe to shared stream (creates new stream if first subscriber, reuses if exists)
        IDeviceStreamSubscription? subscription = null;
        try
        {
            subscription = await _streamManager.SubscribeAsync(deviceId, intervalSeconds, cancellationToken);

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

    #region Authorization helpers (copied from DeviceNotificationHub)

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

    #endregion
}
