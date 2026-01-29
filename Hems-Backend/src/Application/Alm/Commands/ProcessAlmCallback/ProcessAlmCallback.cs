using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.Alm.Commands.ProcessAlmCallback;

public record ProcessAlmCallbackCommand : IRequest<ProcessAlmCallbackResult>
{
    public JsonElement RawBody { get; init; }
}

public class ProcessAlmCallbackResult
{
    public bool Success { get; set; }
    public Guid? JobId { get; set; }
    public Guid? DeviceId { get; set; }
    public int PredictionCount { get; set; }
    public string? Error { get; set; }
}

internal sealed class ProcessAlmCallbackCommandHandler : IRequestHandler<ProcessAlmCallbackCommand, ProcessAlmCallbackResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ProcessAlmCallbackCommandHandler> _logger;

    public ProcessAlmCallbackCommandHandler(
        IApplicationDbContext context,
        ILogger<ProcessAlmCallbackCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProcessAlmCallbackResult> Handle(ProcessAlmCallbackCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var rawJson = request.RawBody.GetRawText();
            _logger.LogInformation("ALM callback received: {Body}", rawJson);

            // Parse callback payload
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Extract job_id from the callback
            string? almJobId = null;
            if (request.RawBody.TryGetProperty("job_id", out var jobIdProp))
                almJobId = jobIdProp.GetString();

            if (string.IsNullOrEmpty(almJobId))
            {
                _logger.LogWarning("ALM callback missing job_id");
                return new ProcessAlmCallbackResult { Success = false, Error = "Missing job_id in callback" };
            }

            // Find the prediction job
            var job = await _context.AlmPredictionJobs
                .FirstOrDefaultAsync(j => j.AlmJobId == almJobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("ALM callback for unknown job: {AlmJobId}", almJobId);
                return new ProcessAlmCallbackResult { Success = false, Error = $"Unknown job: {almJobId}" };
            }

            // Check status from callback
            string? status = null;
            if (request.RawBody.TryGetProperty("status", out var statusProp))
                status = statusProp.GetString();

            if (status == "failed" || status == "error")
            {
                string? errorMsg = null;
                if (request.RawBody.TryGetProperty("error", out var errorProp))
                    errorMsg = errorProp.GetString();

                job.Status = AlmJobStatus.Failed;
                job.ErrorMessage = errorMsg ?? "ALM job failed";
                job.CompletedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("ALM job {AlmJobId} failed: {Error}", almJobId, errorMsg);
                return new ProcessAlmCallbackResult { Success = true, JobId = job.Id, DeviceId = job.DeviceId, Error = errorMsg };
            }

            // Parse prediction results
            var predictionResults = new List<AlmPredictionHistory>();

            if (request.RawBody.TryGetProperty("result", out var resultProp))
            {
                // Result could be an object with predictions array or direct array
                JsonElement predictionsArray;
                if (resultProp.ValueKind == JsonValueKind.Object && resultProp.TryGetProperty("predictions", out var predsProp))
                {
                    predictionsArray = predsProp;
                }
                else if (resultProp.ValueKind == JsonValueKind.Array)
                {
                    predictionsArray = resultProp;
                }
                else
                {
                    _logger.LogWarning("ALM callback result has unexpected format for job {AlmJobId}", almJobId);
                    job.Status = AlmJobStatus.Completed;
                    job.CompletedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    return new ProcessAlmCallbackResult { Success = true, JobId = job.Id, DeviceId = job.DeviceId };
                }

                foreach (var prediction in predictionsArray.EnumerateArray())
                {
                    DateTime predictedTime = default;
                    double predictedValue = 0;

                    // Parse timestamp
                    if (prediction.TryGetProperty("timestamp", out var tsProp))
                    {
                        if (tsProp.ValueKind == JsonValueKind.String)
                            DateTime.TryParse(tsProp.GetString(), out predictedTime);
                    }
                    else if (prediction.TryGetProperty("ds", out var dsProp))
                    {
                        if (dsProp.ValueKind == JsonValueKind.String)
                            DateTime.TryParse(dsProp.GetString(), out predictedTime);
                    }

                    // Parse value
                    if (prediction.TryGetProperty("value", out var valueProp))
                    {
                        predictedValue = valueProp.GetDouble();
                    }
                    else if (prediction.TryGetProperty("yhat", out var yhatProp))
                    {
                        predictedValue = yhatProp.GetDouble();
                    }
                    else if (prediction.TryGetProperty(AlmFeatures.LoadTotalPowerW, out var loadProp))
                    {
                        predictedValue = loadProp.GetDouble();
                    }

                    predictionResults.Add(new AlmPredictionHistory
                    {
                        AlmPredictionJobId = job.Id,
                        DeviceId = job.DeviceId,
                        ModelType = job.ModelType,
                        PredictedTime = predictedTime,
                        PredictedValue = predictedValue,
                        FeatureName = AlmFeatures.LoadTotalPowerW
                    });
                }
            }

            // Save predictions
            if (predictionResults.Count > 0)
            {
                _context.AlmPredictionHistories.AddRange(predictionResults);
            }

            job.Status = AlmJobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "ALM callback processed. JobId: {JobId}, AlmJobId: {AlmJobId}, DeviceId: {DeviceId}, Predictions: {Count}",
                job.Id, almJobId, job.DeviceId, predictionResults.Count);

            return new ProcessAlmCallbackResult
            {
                Success = true,
                JobId = job.Id,
                DeviceId = job.DeviceId,
                PredictionCount = predictionResults.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ALM callback");
            return new ProcessAlmCallbackResult { Success = false, Error = ex.Message };
        }
    }
}
