using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Sites.Commands.CreateSite;
using Hems_Backend.Application.Sites.Commands.DeleteSite;
using Hems_Backend.Application.Sites.Commands.UpdateSite;
using Hems_Backend.Application.Sites.Queries.GetSiteById;
using Hems_Backend.Application.Sites.Queries.GetSites;
using Hems_Backend.Application.Sites.Queries.GetSitesByUser;
using Hems_Backend.Domain.Constants;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// Sites API - Manages user sites and locations.
/// - Admin operations: Full CRUD access to all sites
/// - User operations: CRUD access to own sites
/// </summary>
public class Sites : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // ==== ADMIN ONLY - Full access to all sites ====
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)
            .MapGet(GetSites, "Admin/Sites")
            .MapGet(GetSite, "Admin/Site/{id}")
            .MapDelete(DeleteSiteAdmin, "Admin/Site/{id}");

        // ==== ALL AUTHENTICATED USERS - Own sites ====
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetUserSites, "UserSites")
            .MapPost(CreateSite, "Site")
            .MapPut(UpdateSite, "Site/{id}")
            .MapDelete(DeleteSiteMobile, "Site/{id}");
    }

    // ========================================================================
    // ADMIN ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get all sites (paginated). Admin only.
    /// </summary>
    public async Task<GetSitesResponseDto> GetSites(
        ISender sender,
        [AsParameters] GetSitesQuery query)
    {
        return await sender.Send(query);
    }

    /// <summary>
    /// Get a site by id. Admin only.
    /// </summary>
    public async Task<SiteDto?> GetSite(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetSiteByIdQuery(id));
    }

    /// <summary>
    /// Delete a site by id. Admin only.
    /// </summary>
    public async Task<IResult> DeleteSiteAdmin(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSiteCommand(id));
        return Results.NoContent();
    }

    // ========================================================================
    // USER ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get all sites for the current user.
    /// </summary>
    public async Task<SitesDashboardDto> GetUserSites(
        ISender sender)
    {
        return await sender.Send(new GetSitesByUserQuery());
    }

    /// <summary>
    /// Create a new site.
    /// </summary>
    public async Task<Guid> CreateSite(
        ISender sender,
        CreateSiteCommand command)
    {
        return await sender.Send(command);
    }

    /// <summary>
    /// Update an existing site.
    /// </summary>
    public async Task<IResult> UpdateSite(
        ISender sender,
        Guid id,
        UpdateSiteCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    /// <summary>
    /// Delete a site.
    /// </summary>
    public async Task<IResult> DeleteSiteMobile(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSiteCommand(id));
        return Results.NoContent();
    }
}
