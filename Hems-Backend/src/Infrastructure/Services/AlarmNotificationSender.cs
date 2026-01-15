using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Sends alarm notifications via SignalR
/// </summary>
public class AlarmNotificationSender : IAlarmNotificationSender
{
    private readonly IApplicationDbContext _context;
    private readonly IHubContext<DeviceNotificationHub, IDeviceNotificationHubClient> _hubContext;
    private readonly ILogger<AlarmNotificationSender> _logger;

    // Critical severity levels that trigger SignalR notifications
    private static readonly string[] CriticalSeverities = { "ALARM", "ERROR", "WARNING" };

    public AlarmNotificationSender(
        IApplicationDbContext context,
        IHubContext<DeviceNotificationHub, IDeviceNotificationHubClient> hubContext,
        ILogger<AlarmNotificationSender> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationsAsync(
        List<Guid> createdNotificationIds,
        List<ProcessedNotificationInfo> processedNotifications,
        CancellationToken cancellationToken = default)
    {
        if (!createdNotificationIds.Any())
        {
            return;
        }

        // Create a lookup dictionary from processed notifications
        var processedLookup = processedNotifications.ToDictionary(n => n.Id);

        // Fetch created notifications with related data
        var notifications = await _context.DeviceNotifications
            .Include(dn => dn.DeviceNotificationType)
            .Include(dn => dn.Device)
            .Include(dn => dn.Site)
            .Where(dn => createdNotificationIds.Contains(dn.Id))
            .ToListAsync(cancellationToken);

        foreach (var dn in notifications)
        {
            if (!CriticalSeverities.Contains(dn.DeviceNotificationType.Severity))
            {
                continue;
            }

            // Get processed info for additional details
            var processedInfo = processedLookup.GetValueOrDefault(dn.Id);

            var signalRNotification = new DeviceNotificationDto
            {
                Id = dn.Id,
                DeviceId = dn.DeviceId,
                SiteId = dn.SiteId,
                SetupDeviceId = dn.SetupDeviceId,
                DeviceNotificationTypeId = dn.DeviceNotificationTypeId,
                Timestamp = dn.Timestamp,
                AdditionalData = dn.AdditionalData,
                IsProcessed = dn.IsProcessed,
                Created = dn.Created,
                DeviceNotificationType = new DeviceNotificationTypeDto
                {
                    Id = dn.DeviceNotificationType.Id,
                    Code = dn.DeviceNotificationType.Code,
                    Message = dn.DeviceNotificationType.Message,
                    Description = dn.DeviceNotificationType.Description,
                    Category = dn.DeviceNotificationType.Category,
                    Severity = dn.DeviceNotificationType.Severity,
                    Version = dn.DeviceNotificationType.Version,
                    Manufacturer = dn.DeviceNotificationType.Manufacturer,
                    IsActive = dn.DeviceNotificationType.IsActive
                },
                DeviceManufacturer = dn.Device.Manufacturer,
                SiteName = dn.Site.Name,
                DeviceName = dn.Device.Name,
                // Populate display properties from processed notification info
                Key = processedInfo?.Key ?? dn.DeviceNotificationType.Code,
                DisplayMessage = processedInfo?.Message ?? dn.DeviceNotificationType.Message,
                Severity = processedInfo?.Severity ?? dn.DeviceNotificationType.Severity,
                DetailedDescription = processedInfo?.Description ?? dn.DeviceNotificationType.Description
            };

            try
            {
                await _hubContext.Clients.Group($"Site_{dn.SiteId}").ReceiveDeviceNotification(signalRNotification);
                await _hubContext.Clients.Group($"Device_{dn.DeviceId}").ReceiveDeviceNotification(signalRNotification);

                _logger.LogDebug(
                    "Sent alarm notification {NotificationId} to Site_{SiteId} and Device_{DeviceId}",
                    dn.Id, dn.SiteId, dn.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send SignalR notification {NotificationId}",
                    dn.Id);
            }
        }
    }
}
