using Hems_Backend.Application.Parameters.Commands.CreateParameter;
using Hems_Backend.Application.Parameters.Commands.DeleteParameter;
using Hems_Backend.Application.Parameters.Commands.UpdateParameter;
using Hems_Backend.Application.Parameters.Queries.GetParameter;
using Hems_Backend.Application.Parameters.Queries.GetParameters;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

public class Parameters : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetParameters)
            .MapGet(GetParameter, "{id}")
            .MapPost(CreateParameter)
            .MapPut(UpdateParameter, "{id}")
            .MapDelete(DeleteParameter, "{id}");
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<ParametersVm> GetParameters(
        ISender sender,
        Guid? groupId = null)
    {
        return await sender.Send(new GetParametersQuery { GroupId = groupId });
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<ParameterDto?> GetParameter(
        ISender sender,
        Guid id,
        bool includeGroup = false)
    {
        return await sender.Send(new GetParameterQuery { Id = id, IncludeGroup = includeGroup });
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Created<Guid>> CreateParameter(
        ISender sender,
        CreateParameterCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/Parameters/{id}", id);
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> UpdateParameter(
        ISender sender,
        Guid id,
        UpdateParameterCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> DeleteParameter(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteParameterCommand { Id = id });
        return TypedResults.NoContent();
    }
}
