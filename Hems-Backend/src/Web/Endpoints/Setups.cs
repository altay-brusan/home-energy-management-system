using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Setups.Commands.CreateSetup;
using Hems_Backend.Application.Setups.Commands.DeleteSetup;
using Hems_Backend.Application.Setups.Commands.UpdateSetup;
using Hems_Backend.Application.Setups.Queries.GetSetupById;
using Hems_Backend.Application.Setups.Queries.GetSetups;
using Hems_Backend.Application.Setups.Queries.GetSetupDevicesByUser;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class Setups : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            // Admin endpoints (sadece admin kullanabilir)
            .MapGet(GetSetups)
            .MapGet(GetSetup, "{id}")
            .MapDelete(DeleteSetupAdmin, "{id}")
            // Mobile endpoints (normal kullanıcılar için)
            .MapGet(GetSetupDevice, "mobile/get-setup-device")
            .MapPost(CreateSetup, "mobile/create-setup")
            .MapPut(UpdateSetup, "mobile/update-setup/{id}")
            .MapDelete(DeleteSetupMobile, "mobile/delete-setup/{id}");
    }

    // Admin endpoints
    [Authorize(Policies.AdminOnly)]
    public async Task<PaginatedList<SetupDto>> GetSetups(
        ISender sender,
        [AsParameters] GetSetupsQuery query)
    {
        return await sender.Send(query);
    }

    /// <summary>
    /// Get a setup by id.
    /// NOTE: This endpoint does NOT return the nested device details by default — <c>SetupDto.SetupDevice</c> will be null unless the implementation
    /// explicitly loads the related SetupDevice (for example by using Include or AutoMapper's ProjectTo to fetch the nested data).
    /// </summary>
    /// <param name="sender">Mediator</param>
    /// <param name="id">Setup id</param>
    /// <returns>The requested Setup; device details are not included by default</returns>
    [Authorize(Policies.AdminOnly)]
    public async Task<SetupDto?> GetSetup(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetSetupByIdQuery(id));
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> DeleteSetupAdmin(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSetupCommand(id));
        return Results.NoContent();
    }

    // Mobile endpoints (normal kullanıcılar için)
    [Authorize]
    public async Task<UserSetupDeviceDto?> GetSetupDevice(
        ISender sender,
        Guid siteId)
    {
        return await sender.Send(new GetSetupDevicesByUserQuery { SiteId = siteId });
    }

    [Authorize]
    public async Task<Guid> CreateSetup(
        ISender sender,
        CreateSetupCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize]
    public async Task<IResult> UpdateSetup(
        ISender sender,
        Guid id,
        UpdateSetupCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    [Authorize]
    public async Task<IResult> DeleteSetupMobile(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSetupCommand(id));
        return Results.NoContent();
    }
}




