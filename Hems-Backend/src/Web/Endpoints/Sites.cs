using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Sites.Commands.CreateSite;
using Hems_Backend.Application.Sites.Commands.DeleteSite;
using Hems_Backend.Application.Sites.Commands.UpdateSite;
using Hems_Backend.Application.Sites.Queries.GetSiteById;
using Hems_Backend.Application.Sites.Queries.GetSites;
using Hems_Backend.Application.Sites.Queries.GetSitesByUser;
using Hems_Backend.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class Sites : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            // Admin endpoints (sadece admin kullanabilir)
            .MapGet(GetSites)
            .MapGet(GetSite, "{id}")
            .MapDelete(DeleteSiteAdmin, "{id}")
            // Mobile endpoints (normal kullanıcılar için)
            .MapGet(GetUserSites, "mobile/get-sites")
            .MapPost(CreateSite, "mobile/create-site")
            .MapPut(UpdateSite, "mobile/update-site/{id}")
            .MapDelete(DeleteSiteMobile, "mobile/delete-site/{id}");
    }
    //TODO: TÜM EVLERİN ENERJİSİNİ VE TÜM EVLERİ DÖENN BİR ENDPOİNT GEREK.
    //Ev içerisindede delta enerjisi konacak.
    // Admin endpoints
    [Authorize(Policies.AdminOnly)]
    public async Task<PaginatedList<SiteDto>> GetSites(
        ISender sender,
        [AsParameters] GetSitesQuery query)
    {
        return await sender.Send(query);
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<SiteDto?> GetSite(
        ISender sender,
        Guid id)
    {
        return await sender.Send(new GetSiteByIdQuery(id));
    }

    [Authorize(Policies.AdminOnly)]
    public async Task<IResult> DeleteSiteAdmin(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSiteCommand(id));
        return Results.NoContent();
    }

    // Mobile endpoints (normal kullanıcılar için)
    [Authorize]
    public async Task<List<SiteDto>> GetUserSites(
        ISender sender)
    {
        return await sender.Send(new GetSitesByUserQuery());
    }

    [Authorize]
    public async Task<Guid> CreateSite(
        ISender sender,
        CreateSiteCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize]
    public async Task<IResult> UpdateSite(
        ISender sender,
        Guid id,
        UpdateSiteCommand command)
    {
        if (id != command.Id) return Results.BadRequest();
        await sender.Send(command);
        return Results.NoContent();
    }

    [Authorize]
    public async Task<IResult> DeleteSiteMobile(
        ISender sender,
        Guid id)
    {
        await sender.Send(new DeleteSiteCommand(id));
        return Results.NoContent();
    }
}

