using System.Text.Json;
using Hems_Backend.Application.Alm.Commands.ProcessAlmCallback;
using Hems_Backend.Application.Alm.Commands.RequestPrediction;
using Hems_Backend.Application.Alm.Queries.GetJobResult;
using Hems_Backend.Application.Alm.Queries.GetPredictionHistory;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using Hems_Backend.Domain.Enums;
using Hems_Backend.Infrastructure.Hubs;
using Hems_Backend.Web.Infrastructure;
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
            .MapGet(GetPredictionHistory, "prediction-history/{deviceId:guid}")
            .MapGet(GetJobResult, "job/{jobId:guid}")
            .MapGet(GetAlmHealth, "health");

        // Callback endpoint (no auth - called by ALM service internally)
        app.MapGroup(this)
            .MapPost(HandleCallback, "callback");
    }

    /// <summary>
    /// Request a new prediction for a device
    /// </summary>
    /// <param name="deviceId">The device ID to request prediction for</param>
    /// <param name="sender">MediatR sender</param>
    /// <param name="hubContext">SignalR hub context for notifications</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="modelType">Model type to use (default: power-consumption)</param>
    public async Task<IResult> RequestPrediction(
        Guid deviceId,
        ISender sender,
        IHubContext<AlmNotificationHub, IAlmNotificationHubClient> hubContext,
        ILogger<Alm> logger,
        [FromQuery] string? modelType = "power-consumption")
    {
        logger.LogInformation("Prediction requested for device {DeviceId}, modelType: {ModelType}", deviceId, modelType);

        // Parse model type from query parameter
        var parsedModelType = AlmModelConfiguration.FromDisplayName(modelType) ?? AlmModelType.PowerConsumption;

        var result = await sender.Send(new RequestPredictionCommand
        {
            DeviceId = deviceId,
            ModelType = parsedModelType
        });

        if (!result.Success)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        // Get display name for the model type used
        var modelConfig = AlmModelConfiguration.GetConfig(parsedModelType);

        // Notify frontend via SignalR that prediction has started (or returned from cache)
        if (result.JobId.HasValue)
        {
            var groupName = $"AlmDevice_{deviceId}";
            await hubContext.Clients.Group(groupName).PredictionStarted(new AlmPredictionNotification
            {
                JobId = result.JobId.Value,
                DeviceId = deviceId,
                AlmJobId = result.AlmJobId,
                Status = result.FromCache ? "cached" : "started",
                ModelType = modelConfig.DisplayName,
                FromCache = result.FromCache
            });
        }

        return Results.Ok(new
        {
            jobId = result.JobId,
            almJobId = result.AlmJobId,
            modelType = modelConfig.DisplayName,
            fromCache = result.FromCache,
            message = result.FromCache
                ? "Returning cached prediction from today"
                : "Prediction job submitted successfully"
        });
    }

    /// <summary>
    /// Get prediction history for a device (most recent or specific job)
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
    /// Get the result of a specific prediction job (fetches from AI container)
    /// </summary>
    /// <param name="jobId">The job ID to fetch results for</param>
    /// <param name="sender">MediatR sender</param>
    /// <param name="modelType">Optional model type (default: uses job's stored model type)</param>
    public async Task<IResult> GetJobResult(
        Guid jobId,
        ISender sender,
        [FromQuery] string? modelType = null)
    {
        // Parse model type from query parameter (null means use job's stored model type)
        var parsedModelType = AlmModelConfiguration.FromDisplayName(modelType);

        var result = await sender.Send(new GetJobResultQuery
        {
            JobId = jobId,
            ModelType = parsedModelType
        });

        if (!result.Success)
        {
            return Results.NotFound(new { error = result.Error });
        }

        // Get display name for the model type
        var modelConfig = AlmModelConfiguration.GetConfig(result.ModelType);

        return Results.Ok(new
        {
            jobId = result.JobId,
            deviceId = result.DeviceId,
            almJobId = result.AlmJobId,
            status = result.Status,
            modelType = modelConfig.DisplayName,
            result = result.Result,
            error = result.Error,
            createdAt = result.CreatedAt,
            completedAt = result.CompletedAt
        });
    }

    /// <summary>
    /// ALM service callback endpoint - receives job status updates
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

            // Get display name for the model type
            var modelTypeDisplayName = result.ModelType.HasValue
                ? AlmModelConfiguration.GetConfig(result.ModelType.Value).DisplayName
                : "power-consumption";

            if (result.Status == "completed")
            {
                await hubContext.Clients.Group(groupName).PredictionCompleted(new AlmPredictionNotification
                {
                    JobId = result.JobId.Value,
                    DeviceId = result.DeviceId.Value,
                    AlmJobId = result.AlmJobId,
                    Status = "completed",
                    ModelType = modelTypeDisplayName
                });
            }
            else if (result.Status == "failed" || result.Status == "error")
            {
                await hubContext.Clients.Group(groupName).PredictionFailed(new AlmPredictionNotification
                {
                    JobId = result.JobId.Value,
                    DeviceId = result.DeviceId.Value,
                    AlmJobId = result.AlmJobId,
                    Status = "failed",
                    ModelType = modelTypeDisplayName,
                    Error = result.Error
                });
            }
        }

        return Results.Ok(new { success = result.Success, jobId = result.JobId, status = result.Status });
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
