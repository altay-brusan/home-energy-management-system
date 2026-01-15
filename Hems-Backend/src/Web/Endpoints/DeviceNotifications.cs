using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceNotifications.Commands.CreateDeviceNotification;
using Hems_Backend.Application.DeviceNotifications.Commands.DeleteDeviceNotification;
using Hems_Backend.Application.DeviceNotifications.Commands.MarkAllSiteNotificationsAsRead;
using Hems_Backend.Application.DeviceNotifications.Commands.MarkNotificationAsRead;
using Hems_Backend.Application.DeviceNotifications.Commands.PermanentDeleteDeviceNotification;
using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;
using Hems_Backend.Application.DeviceNotifications.Commands.RestoreDeviceNotification;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Application.DeviceNotifications.Queries.GetDeletedDeviceNotifications;
using Hems_Backend.Application.DeviceNotifications.Queries.GetDeviceNotifications;
using Hems_Backend.Application.DeviceNotifications.Queries.GetDeviceNotificationTypes;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Endpoints;
using Hems_Backend.Web.Infrastructure;
using Hems_Backend.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Web.Endpoints;

public class DeviceNotifications : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Device endpoints (require device-only authorization)
        app.MapGroup(this)
            .RequireAuthorization(Policies.DeviceOnly)
            .MapPost(CreateDeviceNotification);
            // .MapPost(ProcessDeviceAlarm, "alarm"); // Disabled - use DeviceData/Alarm instead

        // User endpoints (require user authorization)
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDeviceNotifications)
            .MapGet(GetDeletedDeviceNotifications, "Deleted")
            .MapPut(MarkDeviceNotificationAsRead, "{id}/MarkRead")
            .MapPut(MarkAllSiteNotificationsAsRead, "MarkAllRead/{siteId}")
            .MapPut(RestoreDeviceNotification, "{id}/Restore")
            .MapDelete(DeleteDeviceNotification, "{id}")
            .MapDelete(PermanentDeleteDeviceNotification, "{id}/Permanent");

        // Admin Tool endpoints (for data management without direct database access)
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetDeviceNotificationTypes, "Tool/Types");
    }

    [Authorize(Policies.DeviceOnly)]
    public static async Task<IResult> CreateDeviceNotification(
        CreateDeviceNotificationCommand command,
        ISender sender,
        IDeviceNotificationService notificationService)
    {
        var result = await sender.Send(command);

        // Only send SignalR notifications for critical severity levels: ALARM, WARNING, ERROR
        var criticalSeverities = new[] { "ALARM", "WARNING", "ERROR" };

        if (criticalSeverities.Contains(result.DeviceNotificationType.Severity))
        {
            // SignalR ile canl? bildirim g�nder
            await notificationService.SendNotificationToSiteAsync(result.SiteId, result);
            await notificationService.SendNotificationToDeviceAsync(result.DeviceId, result);
        }

        return TypedResults.Ok(result);
    }

    [Authorize(Policies.DeviceOnly)]
    public static async Task<IResult> ProcessDeviceAlarm(
        ProcessDeviceAlarmCommand command,
        ISender sender,
        IDeviceNotificationService notificationService,
        IApplicationDbContext context)
    {
        var result = await sender.Send(command);

        // If there are errors but some notifications were created, still notify
        if (result.CreatedNotificationIds.Any())
        {
            // Fetch created notifications to send via SignalR
            var notifications = await context.DeviceNotifications
                .Include(dn => dn.DeviceNotificationType)
                .Include(dn => dn.Device)
                .Include(dn => dn.Site)
                .Where(dn => result.CreatedNotificationIds.Contains(dn.Id))
                .Select(dn => new DeviceNotificationDto
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
                        Status = dn.DeviceNotificationType.Status,
                        Description = dn.DeviceNotificationType.Description,
                        Register = dn.DeviceNotificationType.Register,
                        Value = dn.DeviceNotificationType.Value,
                        Category = dn.DeviceNotificationType.Category,
                        Version = dn.DeviceNotificationType.Version,
                        Manufacturer = dn.DeviceNotificationType.Manufacturer,
                        IsActive = dn.DeviceNotificationType.IsActive
                    },
                    DeviceManufacturer = dn.Device.Manufacturer,
                    SiteName = dn.Site.Name
                })
                .ToListAsync();

            // Send notifications via SignalR for critical severity levels
            var criticalSeverities = new[] { "ALARM", "WARNING", "ERROR" };

            foreach (var notification in notifications)
            {
                if (criticalSeverities.Contains(notification.DeviceNotificationType.Severity))
                {
                    await notificationService.SendNotificationToSiteAsync(notification.SiteId, notification);
                    await notificationService.SendNotificationToDeviceAsync(notification.DeviceId, notification);
                }
            }
        }

        return TypedResults.Ok(new
        {
            success = result.Errors.Count == 0,
            processedCount = result.ProcessedAlarmsCount,
            createdCount = result.CreatedNotificationIds.Count,
            errors = result.Errors
        });
    }

    [Authorize]
    public static async Task<PaginatedList<DeviceNotificationDto>> GetDeviceNotifications(
        ISender sender,
        [AsParameters] GetDeviceNotificationsQuery query)
    {
        return await sender.Send(query);
    }

    [Authorize]
    public static async Task<List<DeviceNotificationTypeDto>> GetDeviceNotificationTypes(
        ISender sender)
    {
        return await sender.Send(new GetDeviceNotificationTypesQuery());
    }

    [Authorize]
    public static async Task<IResult> MarkDeviceNotificationAsRead(
        Guid id,
        ISender sender)
    {
        await sender.Send(new MarkNotificationAsReadCommand { NotificationId = id });
        return TypedResults.NoContent();
    }

    [Authorize]
    public static async Task<IResult> MarkAllSiteNotificationsAsRead(
        Guid siteId,
        ISender sender)
    {
        var affectedCount = await sender.Send(new MarkAllSiteNotificationsAsReadCommand { SiteId = siteId });
        return TypedResults.Ok(new { affectedCount });
    }

    [Authorize]
    public static async Task<PaginatedList<DeviceNotificationDto>> GetDeletedDeviceNotifications(
        ISender sender,
        [AsParameters] GetDeletedDeviceNotificationsQuery query)
    {
        return await sender.Send(query);
    }

    [Authorize]
    public static async Task<IResult> DeleteDeviceNotification(
        Guid id,
        ISender sender)
    {
        await sender.Send(new DeleteDeviceNotificationCommand { NotificationId = id });
        return TypedResults.NoContent();
    }

    [Authorize]
    public static async Task<IResult> RestoreDeviceNotification(
        Guid id,
        ISender sender)
    {
        await sender.Send(new RestoreDeviceNotificationCommand { NotificationId = id });
        return TypedResults.NoContent();
    }

    [Authorize]
    public static async Task<IResult> PermanentDeleteDeviceNotification(
        Guid id,
        ISender sender)
    {
        await sender.Send(new PermanentDeleteDeviceNotificationCommand { NotificationId = id });
        return TypedResults.NoContent();
    }
}
