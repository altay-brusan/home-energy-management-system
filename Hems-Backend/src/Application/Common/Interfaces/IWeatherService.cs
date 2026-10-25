using System.Text.Json.Serialization;

namespace Hems_Backend.Application.Common.Interfaces;

public interface IWeatherService
{
    Task<WeatherData?> GetCurrentWeatherAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
    Task<IEnumerable<WeatherForecastData>> GetWeatherForecastAsync(decimal latitude, decimal longitude, int days = 5, CancellationToken cancellationToken = default);
    Task<WeatherApiResponse?> GetCurrentWeatherWithForecastAsync(decimal latitude, decimal longitude, int days = 5, CancellationToken cancellationToken = default);
}

public class WeatherData
{
    public decimal Temperature { get; set; }
    public decimal FeelsLike { get; set; }
    public decimal Humidity { get; set; }
    public decimal Pressure { get; set; }
    public decimal WindSpeed { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class WeatherForecastData
{
    public DateTime Date { get; set; }
    public decimal Temperature { get; set; }
    public decimal FeelsLike { get; set; }
    public decimal Humidity { get; set; }
    public decimal Pressure { get; set; }
    public decimal WindSpeed { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

// WeatherAPI.com API Response Models
public class WeatherApiResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather Current { get; set; } = new();
    
    [JsonPropertyName("forecast")]
    public ForecastData? Forecast { get; set; }
}

public class WeatherApiForecastResponse
{
    [JsonPropertyName("forecast")]
    public ForecastData Forecast { get; set; } = new();
}

public class CurrentWeather
{
    [JsonPropertyName("temp_c")]
    public decimal TempC { get; set; }
    
    [JsonPropertyName("feelslike_c")]
    public decimal FeelsLikeC { get; set; }
    
    [JsonPropertyName("humidity")]
    public decimal Humidity { get; set; }
    
    [JsonPropertyName("pressure_mb")]
    public decimal PressureMb { get; set; }
    
    [JsonPropertyName("wind_kph")]
    public decimal WindKph { get; set; }
    
    [JsonPropertyName("condition")]
    public WeatherCondition Condition { get; set; } = new();
}

public class WeatherCondition
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}

public class ForecastData
{
    [JsonPropertyName("forecastday")]
    public List<ForecastDay> ForecastDay { get; set; } = new();
}

public class ForecastDay
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;
    
    [JsonPropertyName("day")]
    public DayWeather Day { get; set; } = new();
}

public class DayWeather
{
    [JsonPropertyName("avgtemp_c")]
    public decimal AvgTempC { get; set; }
    
    [JsonPropertyName("avghumidity")]
    public decimal AvgHumidity { get; set; }
    
    [JsonPropertyName("avgpressure_mb")]
    public decimal AvgPressureMb { get; set; }
    
    [JsonPropertyName("maxwind_kph")]
    public decimal MaxWindKph { get; set; }
    
    [JsonPropertyName("condition")]
    public WeatherCondition Condition { get; set; } = new();
}
