namespace Hems_Backend.Application.WeatherForecasts.Queries.GetWeatherForecasts;

public class WeatherForecast
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
