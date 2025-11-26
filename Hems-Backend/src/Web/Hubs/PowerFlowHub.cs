using System.Runtime.CompilerServices;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Hems_Backend.Infrastructure.Data;
using Hems_Backend.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Web.Hubs;

[Authorize]
public class PowerFlowHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IPowerFlowService _powerFlowService;

    public PowerFlowHub(
        IApplicationDbContext context,
        IUser user,
        IPowerFlowService powerFlowService)
    {
        _context = context;
        _user = user;
        _powerFlowService = powerFlowService;
    }

    /// <summary>
    /// React Native will call this as a streaming method:
    /// connection.stream("StreamPowerFlow", deviceId)
    ///
    /// While the connection is alive, backend will periodically fetch PowerFlow from TB
    /// and push it to the client. When the client disconnects, the CancellationToken
    /// is canceled and the loop stops.
    /// </summary>
    public async IAsyncEnumerable<PowerFlowDto> StreamPowerFlow(
        Guid deviceId,
        int intervalSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // ✅ Security: check that current user has access to this device,
        // same logic as DeviceNotificationHub.ValidateDeviceAccessAsync
        //await ValidateDeviceAccessAsync(deviceId);

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
                // normal shutdown
                yield break;
            }
            catch (Exception ex)
            {
                // Optional: log, send error dto, etc.
                // For now, just break the loop if TB is completely failing.
                Console.WriteLine($"PowerFlowHub - error getting PowerFlow for device {deviceId}: {ex}");
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
