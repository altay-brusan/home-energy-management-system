using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Web.Hubs;

/// <summary>
/// SignalR hub for real-time device settings update notifications
/// Frontend subscribes to receive parameter update progress
/// </summary>
[Authorize]
public class DeviceSettingsHub : Hub<IDeviceSettingsHubClient>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeviceSettingsHub(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    /// <summary>
    /// Subscribe to parameter update notifications for a specific device
    /// </summary>
    public async Task SubscribeToDeviceSettings(Guid deviceId)
    {
        await ValidateDeviceAccessAsync(deviceId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetDeviceGroupName(deviceId));
    }

    /// <summary>
    /// Unsubscribe from parameter update notifications
    /// </summary>
    public async Task UnsubscribeFromDeviceSettings(Guid deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetDeviceGroupName(deviceId));
    }

    /// <summary>
    /// Subscribe to a specific batch operation by correlation ID
    /// </summary>
    public async Task SubscribeToBatchOperation(string correlationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetBatchGroupName(correlationId));
    }

    /// <summary>
    /// Unsubscribe from a batch operation
    /// </summary>
    public async Task UnsubscribeFromBatchOperation(string correlationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBatchGroupName(correlationId));
    }

    public static string GetDeviceGroupName(Guid deviceId) => $"DeviceSettings_{deviceId}";
    public static string GetBatchGroupName(string correlationId) => $"BatchOperation_{correlationId}";

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

        // Check if user is Admin - Admins can access all devices
        if (Context.User?.IsInRole("Administrator") == true)
        {
            return;
        }

        var deviceSite = await _context.SetupDevices
            .Where(sd => sd.DeviceId == deviceId)
            .Select(sd => new { sd.Setup.SiteId, sd.Setup.Site.UserId })
            .FirstOrDefaultAsync();

        if (deviceSite == null)
        {
            throw new UnauthorizedAccessException($"Device {deviceId} not found");
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

/// <summary>
/// Strongly-typed client interface for DeviceSettingsHub
/// </summary>
public interface IDeviceSettingsHubClient
{
    Task ParameterUpdateStarted(ParameterUpdateStartedMessage message);
    Task ParameterUpdated(ParameterUpdatedMessage message);
    Task ParameterUpdateFailed(ParameterUpdateFailedMessage message);
    Task ParameterUpdateCompleted(ParameterUpdateCompletedMessage message);
}
