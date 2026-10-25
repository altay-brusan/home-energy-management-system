using Hems_Backend.Application.WeatherForecasts.Queries.GetWeatherForecasts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hems_Backend.Web.Endpoints;

public class WeatherForecasts : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetWeatherForecasts, "{siteId}");
    }

    public async Task<Results<Ok<IEnumerable<WeatherForecast>>, BadRequest<string>, UnauthorizedHttpResult>> 
        GetWeatherForecasts(Guid siteId, ISender sender)
    {
        try
        {
            var forecasts = await sender.Send(new GetWeatherForecastsQuery(siteId));
            return TypedResults.Ok(forecasts);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return TypedResults.BadRequest("An error occurred while fetching weather data");
        }
    }
}