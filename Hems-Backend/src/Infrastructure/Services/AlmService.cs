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

            _logger.LogInformation("Submitting ALM job to /module-tsf-pc, callback: {Callback}, deviceId: {DeviceId}",
                callbackUrl, schema.Id);

            var response = await _httpClient.PostAsync("/module-tsf-pc", content, cancellationToken);
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

    public async Task<AlmJobStatusResponse?> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/module-tsf-pc/jobs/{jobId}", cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ALM job status query returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<AlmJobStatusResponse>(responseBody, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying ALM job status for {JobId}", jobId);
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
}
