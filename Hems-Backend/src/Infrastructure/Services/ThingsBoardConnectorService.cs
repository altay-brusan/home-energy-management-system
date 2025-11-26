using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

public sealed class ThingsBoardConnectorService : IThingsBoardConnectorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThingsBoardConnectorService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiToken;
    private readonly JsonSerializerOptions _jsonOptions;

    public ThingsBoardConnectorService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ThingsBoardConnectorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _baseUrl = configuration["ThingsBoard:BaseUrl"]
            ?? throw new InvalidOperationException("ThingsBoard:BaseUrl not configured");

        _apiToken = configuration["ThingsBoard:ApiToken"]
            ?? throw new InvalidOperationException("ThingsBoard:ApiToken not configured");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("X-Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Authorization", $"Bearer {_apiToken}");
        }
    }

    // ============ DEVICE CRUD OPERATIONS ============

    public async Task<TbDeviceCreateResult> CreateDeviceAsync(
        TbDeviceCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating device in ThingsBoard: {DeviceName}", request.Name);

            // Step 1: Create the device
            var devicePayload = new
            {
                name = request.Name,
                label = request.Label,
                deviceProfileName = request.DeviceProfileName
            };

            var createResponse = await PostAsync<TbDeviceResponse>(
                "/api/device",
                devicePayload,
                cancellationToken);

            if (createResponse?.Id?.Id == null)
            {
                throw new InvalidOperationException("Failed to create device in ThingsBoard");
            }

            var deviceId = createResponse.Id.Id.Value;

            // Step 2: Set server attributes if provided
            if (request.ServerAttributes.Any())
            {
                await SaveDeviceAttributesAsync(deviceId, request.ServerAttributes, cancellationToken);
            }

            _logger.LogInformation(
                "Device created successfully in ThingsBoard. DeviceId: {DeviceId}, Name: {DeviceName}",
                deviceId, request.Name);

            return new TbDeviceCreateResult
            {
                DeviceId = deviceId,
                Name = createResponse.Name ?? request.Name,
                Label = createResponse.Label,
                DeviceProfileName = request.DeviceProfileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device in ThingsBoard: {DeviceName}", request.Name);
            throw;
        }
    }

    public async Task<bool> UpdateDeviceAsync(
        Guid deviceId,
        TbDeviceUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating device in ThingsBoard: {DeviceId}", deviceId);

            // Get existing device first
            var existingDevice = await GetDeviceByIdInternalAsync(deviceId, cancellationToken);
            if (existingDevice == null)
            {
                _logger.LogWarning("Device not found in ThingsBoard: {DeviceId}", deviceId);
                return false;
            }

            // Update only provided fields
            var updatePayload = new
            {
                id = new { id = deviceId, entityType = "DEVICE" },
                name = request.Name ?? existingDevice.Name,
                label = request.Label ?? existingDevice.Label,
                type = existingDevice.Type
            };

            var response = await PostAsync<TbDeviceResponse>(
                "/api/device",
                updatePayload,
                cancellationToken);

            _logger.LogInformation("Device updated successfully in ThingsBoard: {DeviceId}", deviceId);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device in ThingsBoard: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> DeleteDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting device from ThingsBoard: {DeviceId}", deviceId);

            var url = $"/api/device/{deviceId}";
            var response = await _httpClient.DeleteAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Device deleted successfully from ThingsBoard: {DeviceId}", deviceId);
                return true;
            }

            _logger.LogWarning(
                "Failed to delete device from ThingsBoard. DeviceId: {DeviceId}, StatusCode: {StatusCode}",
                deviceId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device from ThingsBoard: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<TbDeviceInfo?> FindDeviceAsync(
        string? serialNumber,
        string? qrCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try QR code first
            if (!string.IsNullOrWhiteSpace(qrCode))
            {
                var byQr = await FindDeviceByNameAsync(qrCode, cancellationToken);
                if (byQr != null)
                {
                    _logger.LogInformation("Found device by QR code: {QrCode} -> {DeviceId}", qrCode, byQr.Id);
                    return byQr;
                }
            }

            // Then try serial number
            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                var bySerial = await FindDeviceByNameAsync(serialNumber, cancellationToken);
                if (bySerial != null)
                {
                    _logger.LogInformation("Found device by serial: {Serial} -> {DeviceId}", serialNumber, bySerial.Id);
                    return bySerial;
                }
            }

            _logger.LogWarning("Device not found. Serial: {Serial}, QR: {Qr}", serialNumber, qrCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding device. Serial: {Serial}, QR: {Qr}", serialNumber, qrCode);
            return null;
        }
    }

    public async Task<TbDeviceInfo?> GetDeviceByIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await GetDeviceByIdInternalAsync(deviceId, cancellationToken);
            if (device == null) return null;

            return new TbDeviceInfo
            {
                Id = device.Id!.Id!.Value,
                Name = device.Name ?? string.Empty,
                Label = device.Label,
                Type = device.Type,
                DeviceProfileName = device.DeviceProfileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device by ID: {DeviceId}", deviceId);
            return null;
        }
    }

    // ============ ATTRIBUTES OPERATIONS ============

    public async Task<bool> SaveDeviceAttributesAsync(
        Guid deviceId,
        Dictionary<string, object> attributes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving attributes for device: {DeviceId}", deviceId);

            var url = $"/api/plugins/telemetry/DEVICE/{deviceId}/attributes/SERVER_SCOPE";
            var response = await PostAsync<object>(url, attributes, cancellationToken);

            _logger.LogInformation("Attributes saved successfully for device: {DeviceId}", deviceId);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving attributes for device: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>?> GetDeviceAttributesAsync(
        Guid deviceId,
        string? keys = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/plugins/telemetry/DEVICE/{deviceId}/values/attributes/SERVER_SCOPE";
            if (!string.IsNullOrWhiteSpace(keys))
            {
                url += $"?keys={Uri.EscapeDataString(keys)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var attributes = JsonSerializer.Deserialize<List<TbAttributeResponse>>(json, _jsonOptions);

            if (attributes == null) return null;

            return attributes.ToDictionary(a => a.Key, a => a.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attributes for device: {DeviceId}", deviceId);
            return null;
        }
    }

    public async Task<bool> DeleteDeviceAttributesAsync(
        Guid deviceId,
        string keys,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/plugins/telemetry/DEVICE/{deviceId}/SERVER_SCOPE?keys={Uri.EscapeDataString(keys)}";
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attributes for device: {DeviceId}", deviceId);
            return false;
        }
    }

    // ============ TELEMETRY OPERATIONS ============

    public async Task<bool> SaveDeviceTelemetryAsync(
        Guid deviceId,
        Dictionary<string, object> telemetry,
        long? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/plugins/telemetry/DEVICE/{deviceId}/timeseries/ANY";
            
            var payload = timestamp.HasValue
                ? new Dictionary<string, object> { ["ts"] = timestamp.Value, ["values"] = telemetry }
                : telemetry;

            var response = await PostAsync<object>(url, payload, cancellationToken);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving telemetry for device: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<Dictionary<string, TbTelemetryValue>?> GetLatestTelemetryAsync(
        Guid deviceId,
        string? keys = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/plugins/telemetry/DEVICE/{deviceId}/values/timeseries";
            if (!string.IsNullOrWhiteSpace(keys))
            {
                url += $"?keys={Uri.EscapeDataString(keys)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var telemetryData = JsonSerializer.Deserialize<Dictionary<string, List<TbTelemetryValueResponse>>>(json, _jsonOptions);

            if (telemetryData == null) return null;

            return telemetryData
                .Where(kv => kv.Value.Any())
                .ToDictionary(
                    kv => kv.Key,
                    kv => new TbTelemetryValue
                    {
                        Ts = kv.Value[0].Ts,
                        Value = kv.Value[0].Value
                    });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest telemetry for device: {DeviceId}", deviceId);
            return null;
        }
    }

    public async Task<Dictionary<string, List<TbTelemetryValue>>?> GetTimeSeriesTelemetryAsync(
        Guid deviceId,
        string keys,
        long startTs,
        long endTs,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/plugins/telemetry/DEVICE/{deviceId}/values/timeseries" +
                     $"?keys={Uri.EscapeDataString(keys)}" +
                     $"&startTs={startTs}" +
                     $"&endTs={endTs}" +
                     $"&limit={limit}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var telemetryData = JsonSerializer.Deserialize<Dictionary<string, List<TbTelemetryValueResponse>>>(json, _jsonOptions);

            if (telemetryData == null) return null;

            return telemetryData.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(v => new TbTelemetryValue
                {
                    Ts = v.Ts,
                    Value = v.Value
                }).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time series telemetry for device: {DeviceId}", deviceId);
            return null;
        }
    }

    // ============ RPC OPERATIONS ============

    public async Task<bool> SendOneWayRpcAsync(
        Guid deviceId,
        string method,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending one-way RPC to device: {DeviceId}, Method: {Method}", deviceId, method);

            var url = $"/api/plugins/rpc/oneway/{deviceId}";
            var payload = new { method, @params = parameters };

            var response = await PostAsync<object>(url, payload, cancellationToken);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending one-way RPC to device: {DeviceId}, Method: {Method}", deviceId, method);
            return false;
        }
    }

    public async Task<TbRpcResponse?> SendTwoWayRpcAsync(
        Guid deviceId,
        string method,
        Dictionary<string, object> parameters,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending two-way RPC to device: {DeviceId}, Method: {Method}", deviceId, method);

            var url = $"/api/plugins/rpc/twoway/{deviceId}?timeout={timeoutMs}";
            var payload = new { method, @params = parameters };

            var response = await PostAsync<Dictionary<string, object>>(url, payload, cancellationToken);

            if (response == null)
            {
                return new TbRpcResponse { Success = false, Error = "No response from device" };
            }

            return new TbRpcResponse
            {
                Success = true,
                Response = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending two-way RPC to device: {DeviceId}, Method: {Method}", deviceId, method);
            return new TbRpcResponse { Success = false, Error = ex.Message };
        }
    }

    // ============ HELPER METHODS ============

    private async Task<TbDeviceInfo?> FindDeviceByNameAsync(string name, CancellationToken cancellationToken)
    {
        var url = $"/api/tenant/devices?deviceName={Uri.EscapeDataString(name)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var device = JsonSerializer.Deserialize<TbDeviceResponse>(json, _jsonOptions);

        if (device?.Id?.Id == null) return null;

        return new TbDeviceInfo
        {
            Id = device.Id.Id.Value,
            Name = device.Name ?? string.Empty,
            Label = device.Label,
            Type = device.Type,
            DeviceProfileName = device.DeviceProfileName
        };
    }

    private async Task<TbDeviceResponse?> GetDeviceByIdInternalAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var url = $"/api/device/{deviceId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TbDeviceResponse>(json, _jsonOptions);
    }

    private async Task<T?> PostAsync<T>(string url, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "ThingsBoard API returned {StatusCode} for {Url}. Error: {Error}",
                response.StatusCode, url, errorContent);
            return default;
        }

        if (typeof(T) == typeof(object)) return default;

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
    }

    // ============ INTERNAL DTOs ============

    private sealed class TbDeviceResponse
    {
        [JsonPropertyName("id")]
        public TbEntityId? Id { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("label")]
        public string? Label { get; set; }
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("deviceProfileName")]
        public string? DeviceProfileName { get; set; }
    }

    private sealed class TbEntityId
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }
        
        [JsonPropertyName("entityType")]
        public string? EntityType { get; set; }
    }

    private sealed class TbAttributeResponse
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        
        [JsonPropertyName("value")]
        public object Value { get; set; } = null!;
        
        [JsonPropertyName("lastUpdateTs")]
        public long LastUpdateTs { get; set; }
    }

    private sealed class TbTelemetryValueResponse
    {
        [JsonPropertyName("ts")]
        public long Ts { get; set; }
        
        [JsonPropertyName("value")]
        public object Value { get; set; } = null!;
    }
}
