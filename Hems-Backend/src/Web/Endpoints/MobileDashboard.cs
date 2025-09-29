using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Mobile.Queries.GetMobileDashboard;
using Hems_Backend.Application.Devices.Queries.CheckDeviceAccess;
using Hems_Backend.Application.DeviceData.Commands.SaveDeviceData;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Hems_Backend.Web.Endpoints.Mobile;

public class MobileDashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDashboard, "mobile/get-dashboard")
            .MapGet(GetDeviceComponentData, "mobile/device/{deviceId:guid}/component/{componentId:guid}/data");
    }

    [Authorize]
    public async Task<MobileDashboardDto> GetDashboard(
        ISender sender)
    {
        return await sender.Send(new GetMobileDashboardQuery());
    }

    [Authorize]
    public async Task<IResult> GetDeviceComponentData(
        Guid deviceId,
        Guid componentId,
        [FromServices] ISender sender,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IConfiguration configuration,
        ClaimsPrincipal user)
    {
        try
        {
            // Kullanıcının bu device'a erişim yetkisi var mı kontrol et
            var hasAccess = await sender.Send(new CheckDeviceAccessQuery 
            { 
                DeviceId = deviceId 
            });

            if (!hasAccess)
            {
                return Results.Forbid();
            }

            // Device Simulator'dan veri al
            using var httpClient = httpClientFactory.CreateClient();
            var deviceSimulatorUrl = configuration["DeviceSimulator:BaseUrl"] ?? "https://localhost:5001";
            var requestUrl = $"{deviceSimulatorUrl}/api/device/generate-data/{deviceId}/{componentId}";
            
            var response = await httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                return Results.Problem("Device Simulator'dan veri alınamadı", statusCode: 500);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var simulatorResponse = JsonSerializer.Deserialize<DeviceSimulatorResponse>(responseContent);

            if (simulatorResponse?.data == null)
            {
                return Results.NotFound("Component verisi bulunamadı");
            }

            // Veriyi DB'ye kaydet
            var saveResult = await sender.Send(new SaveDeviceDataCommand
            {
                DeviceId = deviceId,
                ComponentId = componentId,
                DataType = simulatorResponse.dataType ?? "Unknown",
                Data = JsonSerializer.SerializeToDocument(simulatorResponse.data),
                RequestDate = DateTime.UtcNow
            });

            if (!saveResult.IsSuccess)
            {
                return Results.Problem("Veri kaydedilemedi", statusCode: 500);
            }

            // Sadece Data kısmını döndür
            return Results.Ok(new { data = simulatorResponse.data });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Hata: {ex.Message}", statusCode: 500);
        }
    }
}

// Device Simulator response model
public class DeviceSimulatorResponse
{
    public Guid deviceId { get; set; }
    public Guid componentId { get; set; }
    public string? dataType { get; set; }
    public object? data { get; set; }
    public DateTime timestamp { get; set; }
}
