using Hems_Backend.Application.Locations.Common.Models;
using Hems_Backend.Application.Locations.Queries.GetCountryCities;
using Hems_Backend.Application.Locations.Queries.GetCountries;
using Hems_Backend.Web.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Hems_Backend.Web.Endpoints;

public class Locations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetCountries)
            .MapGet(GetCountryCities, "Cities/{countryId}");
    }

    [Authorize]
    public static async Task<List<CountryDto>> GetCountries(ISender sender)
    {
        return await sender.Send(new GetCountriesQuery());
    }

    [Authorize]
    public static async Task<List<CityDto>> GetCountryCities(
        Guid countryId,
        ISender sender)
    {
        return await sender.Send(new GetCountryCitiesQuery { CountryId = countryId });
    }
}
