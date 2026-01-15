using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Devices.Commands.CreateDevice;
using Hems_Backend.Application.Devices.Commands.DeleteDevice;
using Hems_Backend.Application.Devices.Commands.RegisterDevice;
using Hems_Backend.Application.Devices.Commands.UpdateDevice;
using Hems_Backend.Application.Devices.Queries.GetDeviceById;
//using Hems_Backend.Application.Devices.Queries.GetDevices;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class Devices : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Device login endpoint (no authorization required)
        app.MapGroup(this)
            .MapPost("login", DeviceLogin);

        // Admin Tool endpoints - for managing data without direct database access
        // These are NOT used by the frontend mobile app
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetDevice, "Tool/Device/{id}")
            .MapPost(CreateDevice, "Tool/Device")
            .MapPut(UpdateDevice, "Tool/Device/{id}")
            .MapDelete(DeleteDevice, "Tool/Device/{id}");
    }

    public async Task<DeviceLoginResult> DeviceLogin(
        ISender sender,
        DeviceLoginCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<DeviceDto?> GetDevice(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetDeviceByIdQuery(id));
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Guid> CreateDevice(
        ISender sender,
        CreateDeviceCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> UpdateDevice(
        ISender sender,
        Guid id,
        UpdateDeviceCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> DeleteDevice(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteDeviceCommand(id));
        return Results.NoContent();
    }
}



