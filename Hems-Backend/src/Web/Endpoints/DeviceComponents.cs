using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceComponents.Commands.CreateDeviceComponent;
using Hems_Backend.Application.DeviceComponents.Commands.DeleteDeviceComponent;
using Hems_Backend.Application.DeviceComponents.Commands.UpdateDeviceComponent;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceComponentById;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceComponentsByDeviceId;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class DeviceComponents : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDeviceComponentsByDeviceId, "device/{deviceId}")
            .MapGet(GetDeviceComponent, "{id}")
            .MapPost(CreateDeviceComponent)
            .MapPut(UpdateDeviceComponent, "{id}")
            .MapDelete(DeleteDeviceComponent, "{id}");
    }

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

    [Authorize(Policies.AdminOnly)]
    public async Task<DeviceComponentDto?> GetDeviceComponent(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetDeviceComponentByIdQuery { Id = id });
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Guid> CreateDeviceComponent(
        ISender sender,
        CreateDeviceComponentCommand command)
    {
        return await sender.Send(command);
    }

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

    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> DeleteDeviceComponent(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteDeviceComponentCommand { Id = id });
        return Results.NoContent();
    }
}
