using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hems_Backend.Infrastructure.Services;

public class WeatherApiService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherApiService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public WeatherApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WeatherApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["WeatherApi:ApiKey"] ?? throw new InvalidOperationException("Weather API key not configured");
        _baseUrl = _configuration["WeatherApi:BaseUrl"] ?? throw new InvalidOperationException("Weather API base URL not configured");
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/current.json?key={_apiKey}&q={latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}&aqi=no";
            
            _logger.LogInformation("Fetching current weather for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API returned status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var weatherResponse = JsonSerializer.Deserialize<WeatherApiResponse>(json);

            if (weatherResponse == null)
            {
                _logger.LogWarning("Failed to deserialize weather response");
                return null;
            }

            return new WeatherData
            {
                Temperature = weatherResponse.Current.TempC,
                FeelsLike = weatherResponse.Current.FeelsLikeC,
                Humidity = weatherResponse.Current.Humidity,
                Pressure = weatherResponse.Current.PressureMb,
                WindSpeed = weatherResponse.Current.WindKph / 3.6m, // Convert km/h to m/s
                Description = weatherResponse.Current.Condition.Text,
                Icon = weatherResponse.Current.Condition.Icon,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current weather for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            return null;
        }
    }

    public async Task<IEnumerable<WeatherForecastData>> GetWeatherForecastAsync(decimal latitude, decimal longitude, int days = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/forecast.json?key={_apiKey}&q={latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}&days={days}&aqi=no&alerts=no";
            
            _logger.LogInformation("Fetching weather forecast for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API returned status code: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<WeatherForecastData>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var forecastResponse = JsonSerializer.Deserialize<WeatherApiForecastResponse>(json);

            if (forecastResponse?.Forecast?.ForecastDay == null)
            {
                _logger.LogWarning("Failed to deserialize forecast response");
                return Enumerable.Empty<WeatherForecastData>();
            }

            // Take the requested number of days
            var dailyForecasts = forecastResponse.Forecast.ForecastDay
                .Take(days)
                .Select(day => new WeatherForecastData
                {
                    Date = DateTime.Parse(day.Date),
                    Temperature = day.Day.AvgTempC,
                    FeelsLike = day.Day.AvgTempC, // WeatherAPI doesn't provide feels like for forecast
                    Humidity = day.Day.AvgHumidity,
                    Pressure = day.Day.AvgPressureMb,
                    WindSpeed = day.Day.MaxWindKph / 3.6m, // Convert km/h to m/s
                    Description = day.Day.Condition.Text,
                    Icon = day.Day.Condition.Icon
                });

            return dailyForecasts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather forecast for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            return Enumerable.Empty<WeatherForecastData>();
        }
    }

    public async Task<WeatherApiResponse?> GetCurrentWeatherWithForecastAsync(decimal latitude, decimal longitude, int days = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/forecast.json?key={_apiKey}&q={latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}&days={days}&aqi=no&alerts=no";
            
            _logger.LogInformation("Fetching current weather with forecast for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API returned status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var weatherResponse = JsonSerializer.Deserialize<WeatherApiResponse>(json);

            if (weatherResponse == null)
            {
                _logger.LogWarning("Failed to deserialize weather response");
                return null;
            }

            return weatherResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current weather with forecast for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            return null;
        }
    }
}

