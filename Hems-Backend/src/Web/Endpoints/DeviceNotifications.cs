using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceNotifications.Commands.CreateDeviceNotification;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using Hems_Backend.Application.DeviceNotifications.Queries.GetDeviceNotifications;
using Hems_Backend.Application.DeviceNotifications.Queries.GetDeviceNotificationTypes;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Endpoints;
using Hems_Backend.Web.Infrastructure;
using Hems_Backend.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class DeviceNotifications : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Device endpoints (require device-only authorization)
        app.MapGroup(this)
            .RequireAuthorization(Policies.DeviceOnly)
            .MapPost(CreateDeviceNotification);

        // User endpoints (require user authorization)
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDeviceNotifications)
            .MapGet(GetDeviceNotificationTypes, "types");
    }

    [Authorize(Policies.DeviceOnly)]
    public static async Task<IResult> CreateDeviceNotification(
        CreateDeviceNotificationCommand command,
        ISender sender,
        IDeviceNotificationService notificationService)
    {
        var result = await sender.Send(command);
        
        // SignalR ile canlı bildirim gönder
        await notificationService.SendNotificationToSiteAsync(result.SiteId, result);
        await notificationService.SendNotificationToDeviceAsync(result.DeviceId, result);
        
        return TypedResults.Ok(result);
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
}