using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for sending heartbeat requests to devices via ThingsBoard RPC
/// </summary>
public class HeartbeatSender : IHeartbeatSender
{
    private readonly ILogger<HeartbeatSender> _logger;
    private readonly HttpClient _httpClient;
    private readonly IThingsBoardAuthService _authService;
    private readonly string _tbBaseUrl;

    public HeartbeatSender(
        ILogger<HeartbeatSender> logger,
        HttpClient httpClient,
        IConfiguration configuration,
        IThingsBoardAuthService authService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _authService = authService;
        _tbBaseUrl = configuration["ThingsBoard:BaseUrl"] ?? "http://localhost";
    }

    public async Task<bool> SendHeartbeatAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending heartbeat RPC request to device {DeviceId}", thingsBoardDeviceId);

            var tbDeviceId = thingsBoardDeviceId.ToString();
            var timeoutMs = 5000; // 5 second timeout for heartbeat
            var url = $"{_tbBaseUrl.TrimEnd('/')}/api/plugins/rpc/twoway/{tbDeviceId}?timeout={timeoutMs}";

            // Get JWT token from auth service
            var jwt = await _authService.GetJwtAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            // Send getHeartbeat RPC command
            var body = new { method = "getHeartbeat", @params = new { } };
            request.Content = JsonContent.Create(body);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Device {DeviceId} responded to heartbeat", thingsBoardDeviceId);
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Device {DeviceId} did not respond to heartbeat. Status: {StatusCode}, Error: {Error}",
                    thingsBoardDeviceId, response.StatusCode, errorBody);
                return false;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Heartbeat request to device {DeviceId} timed out", thingsBoardDeviceId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat to device {DeviceId}", thingsBoardDeviceId);
            return false;
        }
    }
}
