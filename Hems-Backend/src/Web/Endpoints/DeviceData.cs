using System.Text.Json;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.DeviceData.Commands.BulkCreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;
using Hems_Backend.Application.DeviceData.Commands.CreateDeviceEnergyData;
using Hems_Backend.Application.DeviceData.Queries.GetDeviceData;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // make sure this is present

namespace Hems_Backend.Web.Endpoints;

public class DeviceData : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Device endpoints (require device-only authorization)
        //app.MapGroup(this)
        //    .RequireAuthorization(Policies.DeviceOnly)
        //    .MapPost(CreateDeviceData)
        //    .MapPost(CreateDeviceEnergyData, "energy")
        //    .MapPost("bulk", BulkCreateDeviceData);

        // User endpoints (require user authorization)
        app.MapGroup(this)
            .MapPost(ReceiveTelemetry, "PowerFlow");

        // User endpoints (require user authorization)
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetDeviceData);
    }

    public async Task<PaginatedList<DeviceDataDto>> GetDeviceData(
        ISender sender,
        [AsParameters] GetDeviceDataQuery query)
    {
        return await sender.Send(query);
    }

    [Authorize(Policies.DeviceOnly)]
    public async Task<int> CreateDeviceData(
        ISender sender,
        CreateDeviceDataCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.DeviceOnly)]
    public async Task<BulkCreateDeviceDataResult> BulkCreateDeviceData(
        ISender sender,
        BulkCreateDeviceDataCommand command)
    {
        return await sender.Send(command);
    }

    [Authorize(Policies.DeviceOnly)]
    public async Task<int> CreateDeviceEnergyData(
        ISender sender,
        CreateDeviceEnergyDataCommand command)
    {
        return await sender.Send(command);
    }


    /// <summary>
    /// Receives ThingBoard telemetry envelopes.
    /// Protected by X-API-Key header via middleware.
    /// For now this just logs the received JSON and returns 200.
    /// </summary>
    public IResult ReceiveTelemetry([FromBody] JsonElement body, ILogger<DeviceData> logger)
    {
        try
        {
            logger.LogInformation("📥 ThingsBoard telemetry received at {UtcNow}: {Payload}", DateTime.UtcNow, body.ToString());

            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("payload", out var inner))
            {
                logger.LogInformation("📦 ThingsBoard inner payload: {InnerPayload}", inner.ToString());
            }

            return Results.Ok(new { message = "Telemetry received" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling ThingsBoard telemetry");
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }


}
