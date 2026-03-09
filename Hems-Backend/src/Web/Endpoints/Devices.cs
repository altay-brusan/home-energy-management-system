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

        // Device management endpoints - Admin and Installer roles
        // These are NOT used by the frontend mobile app
        app.MapGroup(this)
            .RequireAuthorization(Policies.DeviceManagement)
            .MapGet(GetDevice, "Admin/Device/{id}")
            .MapPost(CreateDevice, "Admin/Device")
            .MapPut(UpdateDevice, "Admin/Device/{id}")
            .MapDelete(DeleteDevice, "Admin/Device/{id}");
    }

    public async Task<DeviceLoginResult> DeviceLogin(
        ISender sender,
        DeviceLoginCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.DeviceManagement)]
    public async Task<DeviceDto?> GetDevice(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetDeviceByIdQuery(id));
    }

    [Authorize(Policies.DeviceManagement)]
    public async Task<CreateDeviceResult> CreateDevice(
        ISender sender,
        CreateDeviceCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.DeviceManagement)]
    public async Task<IResult> UpdateDevice(
        ISender sender,
        Guid id,
        UpdateDeviceCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    [Authorize(Policies.DeviceManagement)]
    public async Task<IResult> DeleteDevice(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteDeviceCommand(id));
        return Results.NoContent();
    }
}



