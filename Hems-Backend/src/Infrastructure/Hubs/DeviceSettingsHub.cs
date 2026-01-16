using Hems_Backend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for real-time device settings update notifications
/// Frontend subscribes to receive parameter update progress
/// </summary>
[Authorize]
public class DeviceSettingsHub : Hub<IDeviceSettingsHubClient>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IParameterUpdateQueueService _parameterUpdateQueueService;
    private readonly ILogger<DeviceSettingsHub> _logger;

    public DeviceSettingsHub(
        IApplicationDbContext context,
        IUser user,
        IParameterUpdateQueueService parameterUpdateQueueService,
        ILogger<DeviceSettingsHub> logger)
    {
        _context = context;
        _user = user;
        _parameterUpdateQueueService = parameterUpdateQueueService;
        _logger = logger;
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
    /// Subscribe to a specific batch operation by correlation ID.
    /// If the operation is already in progress or completed, sends the current state immediately.
    /// </summary>
    public async Task SubscribeToBatchOperation(string correlationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetBatchGroupName(correlationId));

        // Check if there's cached state for this operation (for late-subscribing clients)
        var cachedState = _parameterUpdateQueueService.GetOperationState(correlationId);
        if (cachedState != null)
        {
            _logger.LogDebug(
                "Sending cached state to late subscriber for operation {CorrelationId}: Status={Status}, Progress={Current}/{Total}",
                correlationId, cachedState.Status, cachedState.CurrentIndex, cachedState.TotalParameters);

            // Send current state based on operation status
            if (cachedState.Status == "completed" || cachedState.Status == "failed" || cachedState.Status == "cancelled")
            {
                // Operation already completed - send completion message
                await Clients.Caller.ParameterUpdateCompleted(new ParameterUpdateCompletedMessage
                {
                    CorrelationId = cachedState.CorrelationId,
                    DeviceId = cachedState.DeviceId,
                    TotalParameters = cachedState.TotalParameters,
                    SuccessCount = cachedState.SuccessCount,
                    FailedCount = cachedState.FailedCount,
                    SkippedCount = cachedState.SkippedCount,
                    CompletedAt = cachedState.CompletedAt ?? DateTime.UtcNow,
                    Duration = cachedState.Duration ?? TimeSpan.Zero,
                    WasCancelled = cachedState.WasCancelled,
                    FailedParameters = cachedState.FailedParameters.Select(fp => new FailedParameterInfo
                    {
                        ParameterId = fp.ParameterId,
                        ParameterName = fp.ParameterName,
                        ErrorMessage = fp.ErrorMessage,
                        RetryAttempts = fp.RetryAttempts
                    }).ToList()
                });
            }
            else if (cachedState.Status == "in_progress")
            {
                // Operation in progress - send a synthetic ParameterUpdated to show current progress
                // This uses the last successful update info from the cached state
                await Clients.Caller.ParameterUpdated(new ParameterUpdatedMessage
                {
                    CorrelationId = cachedState.CorrelationId,
                    DeviceId = cachedState.DeviceId,
                    ParameterId = Guid.Empty, // Synthetic update for progress sync
                    ParameterName = "[Progress Sync]",
                    NewValue = string.Empty,
                    CurrentIndex = cachedState.CurrentIndex,
                    TotalParameters = cachedState.TotalParameters,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
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
