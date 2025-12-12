using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.CreateSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.DeleteSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.UpdateSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.UpdateDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.InitializeDeviceSettings;
using Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetSetupDeviceParameterValues;
using Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetSetupDeviceParameterValuesTree;
using Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetDeviceParameterValuesTree;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Web.Endpoints;

public class SetupDeviceParameterValues : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetSetupDeviceParameterValues)
            .MapGet(GetSetupDeviceParameterValuesTree, "tree")
            .MapGet(GetDeviceParameterValuesTree, "device/{deviceId}/tree")
            .MapPost(CreateSetupDeviceParameterValue)
            .MapPost(InitializeDeviceSettings, "device/{deviceId}/initialize")
            .MapPut(UpdateSetupDeviceParameterValue, "{id}")
            .MapPut(UpdateDeviceParameterValue, "device/{deviceId}/parameter/{parameterId}")
            .MapDelete(DeleteSetupDeviceParameterValue, "{id}");
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<SetupDeviceParameterValuesVm> GetSetupDeviceParameterValues(
        ISender sender,
        Guid setupDeviceId)
    {
        return await sender.Send(new GetSetupDeviceParameterValuesQuery { SetupDeviceId = setupDeviceId });
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<SetupDeviceParameterValuesTreeVm> GetSetupDeviceParameterValuesTree(
        ISender sender,
        Guid setupDeviceId)
    {
        return await sender.Send(new GetSetupDeviceParameterValuesTreeQuery { SetupDeviceId = setupDeviceId });
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Created<Guid>> CreateSetupDeviceParameterValue(
        ISender sender,
        CreateSetupDeviceParameterValueCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/SetupDeviceParameterValues/{id}", id);
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> UpdateSetupDeviceParameterValue(
        ISender sender,
        Guid id,
        UpdateSetupDeviceParameterValueCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> DeleteSetupDeviceParameterValue(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSetupDeviceParameterValueCommand { Id = id });
        return TypedResults.NoContent();
    }

    public async Task<SetupDeviceParameterValuesTreeVm> GetDeviceParameterValuesTree(
        ISender sender,
        Guid deviceId)
    {
        return await sender.Send(new GetDeviceParameterValuesTreeQuery { DeviceId = deviceId });
    }

    public async Task<Results<NoContent, BadRequest>> UpdateDeviceParameterValue(
        ISender sender,
        Guid deviceId,
        Guid parameterId,
        UpdateDeviceParameterValueCommand command)
    {
        if (deviceId != command.DeviceId || parameterId != command.ParameterId)
            return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Initializes device settings for existing devices that were created before auto-initialization was implemented.
    /// </summary>
    /// <remarks>
    /// This endpoint is for legacy/existing devices only. New devices automatically initialize their settings
    /// when they are linked to a setup via CreateSetupDevice.
    ///
    /// Use this endpoint to:
    /// - Add settings to devices that were created before the auto-initialization feature
    /// - Retry initialization if it failed for some reason
    /// - Manually populate settings for specific devices
    ///
    /// Safety: This endpoint checks if settings already exist and will not overwrite existing data.
    /// </remarks>
    /// <param name="deviceId">The ID of the device to initialize settings for</param>
    /// <returns>Number of settings created, or 0 if settings already exist</returns>
    [Authorize(Policies.AdminOnly)]
    public async Task<Ok<int>> InitializeDeviceSettings(
        ISender sender,
        IApplicationDbContext context,
        Guid deviceId)
    {
        // Find the SetupDevice for this Device
        var setupDevice = await context.SetupDevices
            .Include(sd => sd.Device)
            .Where(sd => sd.DeviceId == deviceId)
            .FirstOrDefaultAsync();

        if (setupDevice == null || setupDevice.Device == null)
        {
            return TypedResults.Ok(0); // No setup device found
        }

        // Initialize settings
        var count = await sender.Send(new InitializeDeviceSettingsCommand
        {
            SetupDeviceId = setupDevice.Id,
            ModelNumber = setupDevice.Device.ModelNumber
        });

        return TypedResults.Ok(count);
    }
}
