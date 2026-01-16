using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Setups.Commands.CreateSetup;
using Hems_Backend.Application.Setups.Commands.DeleteSetup;
using Hems_Backend.Application.Setups.Commands.UpdateSetup;
using Hems_Backend.Application.Setups.Queries.GetSetupById;
using Hems_Backend.Application.Setups.Queries.GetSetups;
using Hems_Backend.Application.Setups.Queries.GetSetupDevicesByUser;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Setups API - Manages device setups and configurations.
/// - Admin operations: Full CRUD access to all setups
/// - User operations: CRUD access to own setups
/// </summary>
public class Setups : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // ==== ADMIN ONLY - Full access to all setups ====
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetSetups, "Admin/Setups")
            .MapGet(GetSetup, "Admin/Setup/{id}")
            .MapDelete(DeleteSetupAdmin, "Admin/Setup/{id}");

        // ==== ALL AUTHENTICATED USERS - Own setups ====
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetSetupDevice, "SetupDevice")
            .MapPost(CreateSetup, "Setup")
            .MapPut(UpdateSetup, "Setup/{id}")
            .MapDelete(DeleteSetupMobile, "Setup/{id}");
    }

    // ========================================================================
    // ADMIN ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get all setups (paginated). Admin only.
    /// </summary>
    public async Task<PaginatedList<SetupDto>> GetSetups(
        ISender sender,
        [AsParameters] GetSetupsQuery query)
    {
        return await sender.Send(query);
    }

    /// <summary>
    /// Get a setup by id. Admin only.
    /// NOTE: This endpoint does NOT return the nested device details by default.
    /// </summary>
    public async Task<SetupDto?> GetSetup(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetSetupByIdQuery(id));
    }

    /// <summary>
    /// Delete a setup by id. Admin only.
    /// </summary>
    public async Task<IResult> DeleteSetupAdmin(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSetupCommand(id));
        return Results.NoContent();
    }

    // ========================================================================
    // USER ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get setup device for the current user by site.
    /// </summary>
    public async Task<UserSetupDeviceDto?> GetSetupDevice(
        ISender sender,
        Guid siteId)
    {
        return await sender.Send(new GetSetupDevicesByUserQuery { SiteId = siteId });
    }

    /// <summary>
    /// Create a new setup.
    /// </summary>
    public async Task<Guid> CreateSetup(
        ISender sender,
        CreateSetupCommand command)
    {
        return await sender.Send(command);
    }

    /// <summary>
    /// Update an existing setup.
    /// </summary>
    public async Task<IResult> UpdateSetup(
        ISender sender,
        Guid id,
        UpdateSetupCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    /// <summary>
    /// Delete a setup.
    /// </summary>
    public async Task<IResult> DeleteSetupMobile(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSetupCommand(id));
        return Results.NoContent();
    }
}
