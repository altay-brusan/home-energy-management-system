using Hems_Backend.Application.SetupDeviceParameterValues.Commands.CreateSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.DeleteSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Commands.UpdateSetupDeviceParameterValue;
using Hems_Backend.Application.SetupDeviceParameterValues.Queries.GetSetupDeviceParameterValues;
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
            .MapPost(CreateSetupDeviceParameterValue)
            .MapPut(UpdateSetupDeviceParameterValue, "{id}")
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
}
