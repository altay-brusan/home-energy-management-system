using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

public class AlmService : IAlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlmService> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public AlmService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AlmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Alm:BaseUrl"] ?? "http://alm-api.alm-service.svc.cluster.local:8000";
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<AlmJobResponse> SubmitPredictionJobAsync(
        string csvData,
        AlmSchema schema,
        string callbackUrl,
        string moduleEndpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build payload matching ALM API expected structure
            var payload = new
            {
                data = csvData,
                schema = new
                {
                    id = schema.Id,
                    timestamp_col = schema.TimestampCol,
                    timestamp_format = schema.TimestampFormat,
                    latitude = schema.Latitude,
                    longitude = schema.Longitude,
                    features = schema.Features,
                    feature_mapping = schema.FeatureMapping
                },
                callback_url = callbackUrl
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Submitting ALM job to {Endpoint}, callback: {Callback}, deviceId: {DeviceId}",
                moduleEndpoint, callbackUrl, schema.Id);

            var response = await _httpClient.PostAsync(moduleEndpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ALM service returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                return new AlmJobResponse
                {
                    Success = false,
                    Error = $"ALM service returned {response.StatusCode}: {responseBody}"
                };
            }

            var result = JsonSerializer.Deserialize<AlmJobApiResponse>(responseBody, _jsonOptions);
            if (result == null)
            {
                return new AlmJobResponse { Success = false, Error = "Failed to parse ALM response" };
            }

            return new AlmJobResponse
            {
                Success = true,
                JobId = result.JobId ?? string.Empty,
                Status = result.Status ?? "queued",
                Position = result.Position,
                Message = result.Message
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("ALM service request timed out");
            return new AlmJobResponse { Success = false, Error = "ALM service request timed out" };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to ALM service at {BaseUrl}", _baseUrl);
            return new AlmJobResponse { Success = false, Error = $"Failed to connect to ALM service: {ex.Message}" };
        }
    }

    public async Task<AlmJobResultResponse?> GetJobResultAsync(
        string moduleEndpoint,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestPath = $"{moduleEndpoint.TrimEnd('/')}/jobs/{jobId}";
            _logger.LogDebug("Fetching ALM job result from {Path}", requestPath);

            var response = await _httpClient.GetAsync(requestPath, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ALM job result query returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                return null;
            }

            // Parse the response from AI container
            var apiResponse = JsonSerializer.Deserialize<AlmJobApiResultResponse>(responseBody, _jsonOptions);
            if (apiResponse == null)
            {
                _logger.LogWarning("Failed to parse ALM job result response");
                return null;
            }

            var result = new AlmJobResultResponse
            {
                JobId = apiResponse.JobId ?? jobId,
                Status = apiResponse.Status ?? "unknown",
                Error = apiResponse.Error,
                CreatedAt = apiResponse.CreatedAt,
                CompletedAt = apiResponse.CompletedAt
            };

            // Parse the result if available
            if (apiResponse.Result.HasValue)
            {
                result.Result = ParseForecastResult(apiResponse.Result.Value);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying ALM job result for {JobId}", jobId);
            return null;
        }
    }

    private AlmForecastResult? ParseForecastResult(JsonElement resultElement)
    {
        try
        {
            var forecastResult = new AlmForecastResult();

            // Parse forecast array
            if (resultElement.TryGetProperty("forecast", out var forecastProp) && forecastProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var point in forecastProp.EnumerateArray())
                {
                    var forecastPoint = new AlmForecastPoint();

                    // Parse timestamp
                    if (point.TryGetProperty("timestamp", out var tsProp))
                    {
                        if (tsProp.ValueKind == JsonValueKind.String && DateTime.TryParse(tsProp.GetString(), out var ts))
                        {
                            forecastPoint.Timestamp = ts;
                        }
                    }

                    // Parse value
                    if (point.TryGetProperty("value", out var valueProp))
                    {
                        forecastPoint.Value = valueProp.GetDouble();
                    }

                    // Parse step
                    if (point.TryGetProperty("step", out var stepProp))
                    {
                        forecastPoint.Step = stepProp.GetInt32();
                    }

                    forecastResult.Forecast.Add(forecastPoint);
                }
            }

            // Parse metadata
            if (resultElement.TryGetProperty("metadata", out var metadataProp))
            {
                forecastResult.Metadata = new AlmForecastMetadata();

                if (metadataProp.TryGetProperty("feature", out var featureProp))
                    forecastResult.Metadata.Feature = featureProp.GetString() ?? string.Empty;

                if (metadataProp.TryGetProperty("interval_seconds", out var intervalProp))
                    forecastResult.Metadata.IntervalSeconds = intervalProp.GetInt32();

                if (metadataProp.TryGetProperty("horizon", out var horizonProp))
                    forecastResult.Metadata.Horizon = horizonProp.GetInt32();

                if (metadataProp.TryGetProperty("unit", out var unitProp))
                    forecastResult.Metadata.Unit = unitProp.GetString() ?? string.Empty;
            }

            // Parse status_code and message if present
            if (resultElement.TryGetProperty("status_code", out var statusCodeProp))
                forecastResult.StatusCode = statusCodeProp.GetString();

            if (resultElement.TryGetProperty("message", out var messageProp))
                forecastResult.Message = messageProp.GetString();

            return forecastResult;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse forecast result");
            return null;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ALM health check failed");
            return false;
        }
    }

    private class AlmJobApiResponse
    {
        public string? JobId { get; set; }
        public string? Status { get; set; }
        public int Position { get; set; }
        public string? Message { get; set; }
    }

    private class AlmJobApiResultResponse
    {
        public string? JobId { get; set; }
        public string? Status { get; set; }
        public JsonElement? Result { get; set; }
        public string? Error { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
