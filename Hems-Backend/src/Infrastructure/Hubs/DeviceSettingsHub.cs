using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for device settings parameter update progress.
/// Clients subscribe to device-level groups to receive real-time progress updates.
/// Supports reconnection with operation status recovery.
/// </summary>
[Authorize]
public class DeviceSettingsHub : Hub<IDeviceSettingsHubClient>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IOperationStateService _operationStateService;
    private readonly ILogger<DeviceSettingsHub> _logger;

    // Track subscriptions per connection
    private static readonly ConcurrentDictionary<string, HashSet<Guid>> _connectionSubscriptions = new();

    public DeviceSettingsHub(
        IApplicationDbContext context,
        IUser user,
        IOperationStateService operationStateService,
        ILogger<DeviceSettingsHub> logger)
    {
        _context = context;
        _user = user;
        _operationStateService = operationStateService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _connectionSubscriptions[Context.ConnectionId] = new HashSet<Guid>();
        _logger.LogInformation(
            "DeviceSettingsHub - Client connected: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, GetUserIdFromContext());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionSubscriptions.TryRemove(Context.ConnectionId, out var deviceIds))
        {
            foreach (var deviceId in deviceIds)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Device_{deviceId}");
            }
            _logger.LogInformation(
                "DeviceSettingsHub - Client disconnected: {ConnectionId}, cleaned up {Count} device subscriptions. Exception: {Exception}",
                Context.ConnectionId, deviceIds.Count, exception?.Message ?? "None");
        }
        else
        {
            _logger.LogInformation(
                "DeviceSettingsHub - Client disconnected: {ConnectionId}. Exception: {Exception}",
                Context.ConnectionId, exception?.Message ?? "None");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to device settings update progress for a specific device.
    /// </summary>
    public async Task SubscribeToDevice(Guid deviceId)
    {
        // Check if already subscribed
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subscriptions))
        {
            if (subscriptions.Contains(deviceId))
            {
                _logger.LogDebug(
                    "DeviceSettingsHub - Already subscribed to device {DeviceId}, skipping",
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
                "DeviceSettingsHub - Access denied for device {DeviceId}: {Message}",
                deviceId, ex.Message);
            throw new HubException(ex.Message);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Device_{deviceId}");

        // Track subscription
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs)
            {
                subs.Add(deviceId);
            }
        }

        _logger.LogInformation(
            "DeviceSettingsHub - Subscribed to device {DeviceId}, connection: {ConnectionId}",
            deviceId, Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from device settings update progress.
    /// </summary>
    public async Task UnsubscribeFromDevice(Guid deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Device_{deviceId}");

        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subs))
        {
            lock (subs)
            {
                subs.Remove(deviceId);
            }
        }

        _logger.LogInformation(
            "DeviceSettingsHub - Unsubscribed from device {DeviceId}, connection: {ConnectionId}",
            deviceId, Context.ConnectionId);
    }

    /// <summary>
    /// Get the status of a specific operation (for reconnection recovery).
    /// </summary>
    public Task<OperationState?> GetOperationStatus(string operationId)
    {
        var state = _operationStateService.GetOperation(operationId);
        return Task.FromResult(state);
    }

    /// <summary>
    /// Get the active operation for a device (for reconnection recovery).
    /// </summary>
    public async Task<OperationState?> GetActiveOperation(Guid deviceId)
    {
        // Validate access first
        try
        {
            await ValidateDeviceAccessAsync(deviceId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "DeviceSettingsHub - Access denied for GetActiveOperation on device {DeviceId}: {Message}",
                deviceId, ex.Message);
            throw new HubException(ex.Message);
        }

        return _operationStateService.GetActiveOperationForDevice(deviceId);
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

        // Admin can access all devices
        if (Context.User?.IsInRole("Administrator") == true)
        {
            _logger.LogDebug(
                "DeviceSettingsHub - Admin user {UserId} granted access to device {DeviceId}",
                userId, deviceId);
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
}
