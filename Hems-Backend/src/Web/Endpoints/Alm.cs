using System.Text.Json;
using Hems_Backend.Application.Alm.Commands.ProcessAlmCallback;
using Hems_Backend.Application.Alm.Commands.RequestPrediction;
using Hems_Backend.Application.Alm.Queries.GetPredictionHistory;
using Hems_Backend.Application.Alm.Queries.GetPredictionJobStatus;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Hubs;
using Hems_Backend.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Hems_Backend.Web.Endpoints;

public class Alm : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Authenticated endpoints (user-facing)
        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(RequestPrediction, "predict/{deviceId:guid}")
            .MapGet(GetPredictionHistory, "predictions/{deviceId:guid}")
            .MapGet(GetPredictionJobStatus, "job/{jobId:guid}")
            .MapGet(GetAlmHealth, "health");

        // Callback endpoint (no auth - called by ALM service internally)
        app.MapGroup(this)
            .MapPost(HandleCallback, "callback");
    }

    /// <summary>
    /// Request a new prediction for a device
    /// </summary>
    public async Task<IResult> RequestPrediction(
        Guid deviceId,
        ISender sender,
        ILogger<Alm> logger)
    {
        logger.LogInformation("Prediction requested for device {DeviceId}", deviceId);

        var result = await sender.Send(new RequestPredictionCommand { DeviceId = deviceId });

        if (!result.Success)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Ok(new
        {
            jobId = result.JobId,
            almJobId = result.AlmJobId,
            message = "Prediction job submitted successfully"
        });
    }

    /// <summary>
    /// Get prediction results for a device (most recent or specific job)
    /// </summary>
    public async Task<IResult> GetPredictionHistory(
        Guid deviceId,
        ISender sender,
        [FromQuery] Guid? jobId = null)
    {
        var result = await sender.Send(new GetPredictionHistoryQuery
        {
            DeviceId = deviceId,
            JobId = jobId
        });

        if (!result.Success)
        {
            return Results.NotFound(new { error = result.Error });
        }

        return Results.Ok(result);
    }

    /// <summary>
    /// Get the status of a specific prediction job
    /// </summary>
    public async Task<IResult> GetPredictionJobStatus(
        Guid jobId,
        ISender sender)
    {
        var result = await sender.Send(new GetPredictionJobStatusQuery { JobId = jobId });

        if (!result.Success)
        {
            return Results.NotFound(new { error = result.Error });
        }

        return Results.Ok(result);
    }

    /// <summary>
    /// ALM service callback endpoint - receives prediction results
    /// </summary>
    public async Task<IResult> HandleCallback(
        [FromBody] JsonElement body,
        ISender sender,
        IHubContext<AlmNotificationHub, IAlmNotificationHubClient> hubContext,
        ILogger<Alm> logger)
    {
        logger.LogInformation("ALM callback received");

        var result = await sender.Send(new ProcessAlmCallbackCommand { RawBody = body });

        // Notify frontend via SignalR
        if (result.DeviceId.HasValue && result.JobId.HasValue)
        {
            var groupName = $"AlmDevice_{result.DeviceId.Value}";

            if (result.Success && string.IsNullOrEmpty(result.Error))
            {
                await hubContext.Clients.Group(groupName).PredictionCompleted(new AlmPredictionNotification
                {
                    JobId = result.JobId.Value,
                    DeviceId = result.DeviceId.Value,
                    Status = "completed",
                    PredictionCount = result.PredictionCount
                });
            }
            else
            {
                await hubContext.Clients.Group(groupName).PredictionFailed(new AlmPredictionNotification
                {
                    JobId = result.JobId.Value,
                    DeviceId = result.DeviceId.Value,
                    Status = "failed",
                    Error = result.Error
                });
            }
        }

        return Results.Ok(new { success = result.Success, jobId = result.JobId });
    }

    /// <summary>
    /// Check ALM service health status
    /// </summary>
    public async Task<IResult> GetAlmHealth(
        IAlmService almService)
    {
        var isHealthy = await almService.IsHealthyAsync();
        return Results.Ok(new { healthy = isHealthy, timestamp = DateTimeOffset.UtcNow });
    }
}
