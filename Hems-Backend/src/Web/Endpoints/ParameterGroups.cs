using Hems_Backend.Application.ParameterGroups.Commands.CreateParameterGroup;
using Hems_Backend.Application.ParameterGroups.Commands.DeleteParameterGroup;
using Hems_Backend.Application.ParameterGroups.Commands.UpdateParameterGroup;
using Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroup;
using Hems_Backend.Application.ParameterGroups.Queries.GetParameterGroups;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

public class ParameterGroups : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetParameterGroups)
            .MapGet(GetParameterGroup, "{id}")
            .MapPost(CreateParameterGroup)
            .MapPut(UpdateParameterGroup, "{id}")
            .MapDelete(DeleteParameterGroup, "{id}");
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<ParameterGroupsVm> GetParameterGroups(
        ISender sender)
    {
        return await sender.Send(new GetParameterGroupsQuery());
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<ParameterGroupDto?> GetParameterGroup(
        ISender sender,
        Guid id,
        bool includeChildren = false)
    {
        return await sender.Send(new GetParameterGroupQuery { Id = id, IncludeChildren = includeChildren });
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Created<Guid>> CreateParameterGroup(
        ISender sender,
        CreateParameterGroupCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/ParameterGroups/{id}", id);
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> UpdateParameterGroup(
        ISender sender,
        Guid id,
        UpdateParameterGroupCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> DeleteParameterGroup(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteParameterGroupCommand { Id = id });
        return TypedResults.NoContent();
    }
}
