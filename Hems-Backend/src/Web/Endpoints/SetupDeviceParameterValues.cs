using Hems_Backend.Application.SetupDeviceParameterValues.Commands.CreateSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.DeleteSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.UpdateSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.UpdateDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetSetupDeviceParameterValues;
using Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetSetupDeviceParameterValuesTree;
using Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetDeviceParameterValuesTree;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

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
            .MapPut(UpdateSetupDeviceParameterValue, "{id}")
            .MapPut(UpdateDeviceParameterValue, "device/{deviceId}/parameter/{parameterId}")
            .MapDelete(DeleteSetupDeviceParameterValue, "{id}");
    }

    public async Task<SetupDeviceParameterValuesVm> GetSetupDeviceParameterValues(
        ISender sender,
        Guid setupDeviceId)
    {
        return await sender.Send(new GetSetupDeviceParameterValuesQuery { SetupDeviceId = setupDeviceId });
    }

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
}
