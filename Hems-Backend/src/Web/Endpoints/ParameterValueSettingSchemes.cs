using Hems_Backend.Application.ParameterValueSettingSchemes.Commands.CreateParameterValueSettingScheme;
using Hems_Backend.Application.ParameterValueSettingSchemes.Commands.DeleteParameterValueSettingScheme;
using Hems_Backend.Application.ParameterValueSettingSchemes.Commands.UpdateParameterValueSettingScheme;
using Hems_Backend.Application.ParameterValueSettingSchemes.Queries.GetParameterValueSettingSchemes;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

public class ParameterValueSettingSchemes : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetParameterValueSettingSchemes)
            .MapPost(CreateParameterValueSettingScheme)
            .MapPut(UpdateParameterValueSettingScheme, "{id}")
            .MapDelete(DeleteParameterValueSettingScheme, "{id}");
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<ParameterValueSettingSchemesVm> GetParameterValueSettingSchemes(
        ISender sender,
        string? modelNumber = null)
    {
        return await sender.Send(new GetParameterValueSettingSchemesQuery { ModelNumber = modelNumber });
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Created<Guid>> CreateParameterValueSettingScheme(
        ISender sender,
        CreateParameterValueSettingSchemeCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/api/ParameterValueSettingSchemes/{id}", id);
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> UpdateParameterValueSettingScheme(
        ISender sender,
        Guid id,
        UpdateParameterValueSettingSchemeCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<Results<NoContent, BadRequest>> DeleteParameterValueSettingScheme(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteParameterValueSettingSchemeCommand { Id = id });
        return TypedResults.NoContent();
    }
}
