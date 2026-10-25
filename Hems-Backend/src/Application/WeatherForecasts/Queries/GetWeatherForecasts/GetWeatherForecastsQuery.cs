using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.WeatherForecasts.Queries.GetWeatherForecasts;

public record GetWeatherForecastsQuery(Guid SiteId) : IRequest<IEnumerable<WeatherForecast>>;

public class GetWeatherForecastsQueryHandler : IRequestHandler<GetWeatherForecastsQuery, IEnumerable<WeatherForecast>>
{
    private readonly IApplicationDbContext _context;
    private readonly IWeatherService _weatherService;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public GetWeatherForecastsQueryHandler(
        IApplicationDbContext context,
        IWeatherService weatherService,
        IUser currentUser,
        IIdentityService identityService)
    {
        _context = context;
        _weatherService = weatherService;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<IEnumerable<WeatherForecast>> Handle(GetWeatherForecastsQuery request, CancellationToken cancellationToken)
    {
        // Admin kullanıcılar tüm site'lara erişebilir - Site kontrolünü önce yapalım
        bool hasAccess = false;
        
        if (_currentUser.Id != null)
        {
            var isAdmin = await _identityService.IsInRoleAsync(_currentUser.Id, "Administrator");
            if (isAdmin)
            {
                // Admin ise herhangi bir site'a erişebilir
                hasAccess = true;
            }
            else
            {
                // Normal kullanıcılar için site sahipliği veya paylaşım kontrolü
                hasAccess = await _context.Sites
                    .AnyAsync(s => s.Id == request.SiteId && 
                                 (s.UserId == _currentUser.Id || 
                                  _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == _currentUser.Id)), 
                             cancellationToken);
            }
        }

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Site not found or access denied");
        }

        // Site'ı bul
        var site = await _context.Sites
            .FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken);

        if (site == null)
        {
            throw new UnauthorizedAccessException("Site not found");
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
