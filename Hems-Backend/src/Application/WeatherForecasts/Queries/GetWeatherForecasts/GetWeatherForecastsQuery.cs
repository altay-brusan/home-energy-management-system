using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.WeatherForecasts.Queries.GetWeatherForecasts;

public record GetWeatherForecastsQuery(Guid SiteId) : IRequest<IEnumerable<WeatherForecast>>;

public class GetWeatherForecastsQueryHandler : IRequestHandler<GetWeatherForecastsQuery, IEnumerable<WeatherForecast>>
{
    private readonly IApplicationDbContext _context;
    private readonly IWeatherService _weatherService;
    private readonly IUser _currentUser;

    public GetWeatherForecastsQueryHandler(
        IApplicationDbContext context,
        IWeatherService weatherService,
        IUser currentUser)
    {
        _context = context;
        _weatherService = weatherService;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<WeatherForecast>> Handle(GetWeatherForecastsQuery request, CancellationToken cancellationToken)
    {
        // Site'ı bul ve kullanıcının erişim yetkisini kontrol et

        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == request.SiteId && 
                                     (s.UserId == _currentUser.Id || 
                                      _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == _currentUser.Id)), 
                                 cancellationToken);

        if (site == null)
        {
            throw new UnauthorizedAccessException("Site not found or access denied");
        }

        if (site.Latitude == null || site.Longitude == null)
        {
            throw new InvalidOperationException("Site coordinates are not set");
        }

            // Sadece current weather verilerini al
            var currentWeather = await _weatherService.GetCurrentWeatherAsync(
                site.Latitude.Value, 
                site.Longitude.Value, 
                cancellationToken);

            if (currentWeather == null)
            {
                return Enumerable.Empty<WeatherForecast>();
            }

            // Current weather'ı döndür
            var weatherForecast = new WeatherForecast
            {
                Date = DateTime.Now.Date,
                Temperature = currentWeather.Temperature,
                FeelsLike = currentWeather.FeelsLike,
                Humidity = currentWeather.Humidity,
                Pressure = currentWeather.Pressure,
                WindSpeed = currentWeather.WindSpeed,
                Description = currentWeather.Description,
                Icon = currentWeather.Icon
            };

            return new[] { weatherForecast };
    }
}
