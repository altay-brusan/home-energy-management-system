namespace Hems_Backend.Web.Services;
using System.Net.Http.Json;
using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Hems_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public interface IPowerFlowService
{
    /// <summary>
    /// Calls ThingsBoard (via RPC) and returns the latest PowerFlow data for a device.
    /// Implementation will typically call TB REST: /api/plugins/rpc/twoway/{deviceId}
    /// and map PowerFlowState JSON -> PowerFlowDto.
    /// </summary>
    Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, CancellationToken cancellationToken = default);
}



public class PowerFlowService : IPowerFlowService
{
    private readonly IApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IThingsBoardAuthService _authService;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly string _tbBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public PowerFlowService(
        IApplicationDbContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        IThingsBoardAuthService authService,
        IDeviceMappingService deviceMapping)
    {
        _context = context;
        _httpClient = httpClient;
        _authService = authService;
        _deviceMapping = deviceMapping;

        _tbBaseUrl = configuration["ThingsBoard:BaseUrl"]
                     ?? "http://localhost";   // fallback for dev

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        // Map local deviceId to ThingsBoardDeviceId (with caching)
        var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(deviceId, cancellationToken);
        
        if (thingsBoardDeviceId == null)
        {
            Console.WriteLine($"Device mapping not found for deviceId: {deviceId}");
            return null;
        }

        var tbDeviceId = thingsBoardDeviceId.Value.ToString();
        var timeoutMs = 10000;
        var url = $"{_tbBaseUrl.TrimEnd('/')}/api/plugins/rpc/twoway/{tbDeviceId}?timeout={timeoutMs}";

        // 🔑 Get (or refresh) JWT from auth service
        var jwt = await _authService.GetJwtAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var body = new { method = "getPowerFlow", @params = new { } };
        request.Content = JsonContent.Create(body);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"TB RPC error ({response.StatusCode}): {errorBody}");
            return null;
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var rpcState = await JsonSerializer.DeserializeAsync<PowerFlowStateRpcDto>(
            stream, _jsonOptions, cancellationToken);

        if (rpcState == null) return null;

        return MapToPowerFlowDto(deviceId, rpcState);
    }

    private static PowerFlowDto MapToPowerFlowDto(Guid deviceId, PowerFlowStateRpcDto state)
    {
        // safe helpers
        static double ToKw(int? w) => w.HasValue ? w.Value / 1000.0 : 0.0;

        return new PowerFlowDto
        {
            DeviceId = deviceId,
            TimestampUtc = state.TimestampUtc == default
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(state.TimestampUtc, TimeSpan.Zero),

            SolarPowerKw = ToKw(state.Pv?.TotalPowerW),
            GridPowerKw = ToKw(state.Grid?.TotalPowerW),
            LoadPowerKw = ToKw(state.Load?.TotalPowerW),
            BatteryPowerKw = ToKw(state.Battery?.PowerW),
            GeneratorPowerKw = ToKw(state.Generator?.TotalPowerW),

            RawJson = null // you can fill this if you need debugging
        };
    }

    #region Internal DTOs to match Node-RED PowerFlowState JSON

    private sealed class PowerFlowStateRpcDto
    {
        public string? Kind { get; init; }
        public DateTime TimestampUtc { get; init; }
        public int Version { get; init; }
        public string? DeviceId { get; init; }
        public PvDto? Pv { get; init; }
        public GridDto? Grid { get; init; }
        public LoadDto? Load { get; init; }
        public BatteryDto? Battery { get; init; }
        public GeneratorDto? Generator { get; init; }
    }

    private sealed class PvDto
    {
        public int? TotalPowerW { get; init; }
        public List<MpptDto>? Mppt { get; init; }
    }

    private sealed class MpptDto
    {
        public int Id { get; init; }
        public int? PowerW { get; init; }
    }

    private sealed class GridDto
    {
        public int? TotalPowerW { get; init; }
    }

    private sealed class LoadDto
    {
        public int? TotalPowerW { get; init; }
    }

    private sealed class BatteryDto
    {
        public int? PowerW { get; init; }
    }

    private sealed class GeneratorDto
    {
        public int? TotalPowerW { get; init; }
    }

    #endregion
}


