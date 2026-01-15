using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceComponents.Commands.CreateDeviceComponent;
using Hems_Backend.Application.DeviceComponents.Commands.DeleteDeviceComponent;
using Hems_Backend.Application.DeviceComponents.Commands.UpdateDeviceComponent;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceComponentById;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceComponentsByDeviceId;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceSummary;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceDetails;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hems_Backend.Web.Endpoints;

public class DeviceComponents : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            // User-facing endpoints (used by mobile app)
            .MapGet(GetDeviceSummary, "DeviceSummary")
            .MapGet(GetDeviceDetails, "Details/{componentId}")
            // Admin endpoints - for managing data without direct database access
            // These endpoints are not used by the mobile app, they are administrative utilities
            .MapGet(GetDeviceComponentsByDeviceId, "Admin/DeviceComponents/{deviceId}")
            .MapGet(GetDeviceComponent, "Admin/Component/{id}")
            .MapPost(CreateDeviceComponent, "Admin/Component")
            .MapPut(UpdateDeviceComponent, "Admin/Component/{id}")
            .MapDelete(DeleteDeviceComponent, "Admin/Component/{id}");
    }

    /// <summary>
    /// Admin: Get all device components for a specific device.
    /// Used for administrative data management without direct database access.
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<List<DeviceComponentDto>> GetDeviceComponentsByDeviceId(
        ISender sender,
        Guid deviceId,
        bool? isDeleted = null)
    {
        return await sender.Send(new GetDeviceComponentsByDeviceIdQuery 
        { 
            DeviceId = deviceId, 
            IsDeleted = isDeleted 
        });
    }

    /// <summary>
    /// Admin: Get a device component by its ID.
    /// Used for administrative data management without direct database access.
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<DeviceComponentDto?> GetDeviceComponent(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetDeviceComponentByIdQuery { Id = id });
    }

    /// <summary>
    /// Admin: Create a new device component.
    /// Used for administrative data management without direct database access.
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<Guid> CreateDeviceComponent(
        ISender sender,
        CreateDeviceComponentCommand command)
    {
        return await sender.Send(command);
    }

    /// <summary>
    /// Admin: Update an existing device component.
    /// Used for administrative data management without direct database access.
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> UpdateDeviceComponent(
        ISender sender,
        Guid id,
        UpdateDeviceComponentCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    /// <summary>
    /// Admin: Delete a device component.
    /// Used for administrative data management without direct database access.
    /// </summary>
    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> DeleteDeviceComponent(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteDeviceComponentCommand { Id = id });
        return Results.NoContent();
    }

    [Authorize]
    public async Task<DeviceSummaryResponseDto> GetDeviceSummary(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetDeviceSummaryQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<DeviceDetailsResponseDto?> GetDeviceDetails(
        ISender sender,
        Guid componentId)
    {
        var query = new GetDeviceDetailsQuery
        {
            ComponentId = componentId
        };

        return await sender.Send(query);
    }
}
