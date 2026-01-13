// Weather API Types

export interface WeatherForecast {
  date: string;
  temperature: number;
  feelsLike: number;
  humidity: number;
  pressure: number;
  windSpeed: number;
  description: string;
  icon: string;
}

export interface GetWeatherForecastsParams {
  siteId: string;
}
